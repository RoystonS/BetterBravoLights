using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Timers;
using BravoLights.Ast;
using BravoLights.Common;
using Microsoft.FlightSimulator.SimConnect;

namespace BravoLights.Connections
{   
    enum EventId: uint
    {
        SimState = 1,
    }

    enum DefineId : uint
    {
        WASMRequestResponse = 1,
        WASMLVars = 2,

        // The start of dynamically-allocated definitions for simulator variables
        DynamicStart = 100
    }

    enum RequestId : uint
    {
        SimState = 1,
        WASMResponse = 2,
        WASMLVars = 3,
        AircraftLoaded = 4,

        // The start of dynamically-allocated request ids for simulator variables
        DynamicStart = DefineId.DynamicStart
    }

    enum WASMReaderState
    {
        Neutral,
        ReadingLVars
    }

    class SimConnectConnection : IConnection, IWASMChannel
    {
        // Names of the client data areas established by the WASM module
        private const string CDA_NAME_SIMVAR = "BetterBravoLights.LVars";
        private const string CDA_NAME_REQUEST = "BetterBravoLights.Request";
        private const string CDA_NAME_RESPONSE = "BetterBravoLights.Response";

        // Ids for the client data areas
        private enum ClientDataId
        {
            LVars = 0,
            Request = 1,
            Response = 2
        }

        private const string RESPONSE_LVAR_START = "!LVARS-START";
        private const string RESPONSE_LVAR_END = "!LVARS-END";

        // Size of the request and response CDAs
        private const int RequestResponseSize = 256;

        [StructLayout(LayoutKind.Sequential)]
        public struct RequestString
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = RequestResponseSize)]
            public byte[] data;

            public RequestString(string str)
            {
                var bytes = Encoding.ASCII.GetBytes(str);
                var ret = new byte[RequestResponseSize];
                Array.Copy(bytes, ret, bytes.Length);
                data = ret;
            }
        }

        public struct ResponseString
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = RequestResponseSize)]
            public string Data;
        }

        // This MUST match the value in the WASM
        private const int MaxDataValuesInPacket = 10;

        public struct LVarData : ILVarData
        {
            [MarshalAs(UnmanagedType.U2)]
            public short ValueCount;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = MaxDataValuesInPacket, ArraySubType = UnmanagedType.U2)]
            public short[] Ids;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = MaxDataValuesInPacket, ArraySubType = UnmanagedType.R8)]
            public double[] Values;

            short ILVarData.ValueCount => ValueCount;

            short[] ILVarData.Ids => Ids;

            double[] ILVarData.Values => Values;
        }

        private readonly uint LVarDataSize = (uint)Marshal.SizeOf<LVarData>();

        public static SimConnectConnection Connection = new();

        private readonly Timer reconnectTimer = new();
        private SimState simState = SimState.SimStopped;

        private SimConnectConnection()
        {
            reconnectTimer.AutoReset = false;
            reconnectTimer.Interval = new TimeSpan(0, 0, 30).TotalMilliseconds;
            reconnectTimer.Elapsed += Timer_Elapsed;

            LVarManager.Connection.SetWASMChannel(this);
        }

        public void Start()
        {
            ConnectNow();
        }

        private uint nextVariableId = (uint)DefineId.DynamicStart;

        private readonly Dictionary<uint, NameAndUnits> idToName = new();
        private readonly Dictionary<NameAndUnits, uint> nameToId = new(new NameAndUnitsComparer());
        private readonly Dictionary<NameAndUnits, ISet<EventHandler<ValueChangedEventArgs>>> variableHandlers =
            new(new NameAndUnitsComparer());
        private readonly Dictionary<NameAndUnits, double> lastReportedValue = new(new NameAndUnitsComparer());

        private void SubscribeToSimConnect(NameAndUnits nameAndUnits)
        {
            var id = ++nextVariableId;

            idToName[id] = nameAndUnits;
            nameToId[nameAndUnits] = id;

            simconnect.AddToDataDefinition((DefineId)id, nameAndUnits.Name, nameAndUnits.Units, SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
            simconnect.RegisterDataDefineStruct<ContainerStruct>((DefineId)id);
            simconnect.RequestDataOnSimObject((RequestId)id, (DefineId)id, 0, SIMCONNECT_PERIOD.SIM_FRAME, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);
        }

        private void UnsubscribeFromSimConnect(SimVarExpression simvar)
        {
            var name = simvar.NameAndUnits;
            uint id = 0;
            try
            {
                id = nameToId[name];
                simconnect.ClearClientDataDefinition((DefineId)id);
            } catch
            {
                nameToId.Remove(name);
                if (id > 0)
                {
                    idToName.Remove(id);
                }
            }
        }


        public void SimulateExit()
        {
            this.RaiseSimStateChanged(SimState.SimExited);
        }

        public void AddListener(IVariable variable, EventHandler<ValueChangedEventArgs> handler)
        {
            var simvar = (SimVarExpression)variable;
            var nau = simvar.NameAndUnits;

            lock (this)
            {
                if (!variableHandlers.TryGetValue(nau, out ISet<EventHandler<ValueChangedEventArgs>> handlers))
                {
                    handlers = new HashSet<EventHandler<ValueChangedEventArgs>>();
                    variableHandlers.Add(nau, handlers);
                }

                if (handlers.Count == 0)
                {
                    if (this.simconnect != null)
                    {
                        SubscribeToSimConnect(nau);
                    }
                    else if (!reconnectTimer.Enabled)
                    {
                        ConnectNow();
                    }
                }

                handlers.Add(handler);

                SendLastValue(variable, handler);
            }
        }

        private void SendLastValue(IVariable variable, EventHandler<ValueChangedEventArgs> handler)
        {
            var simvar = (SimVarExpression)variable;
            var nau = simvar.NameAndUnits;
            if (simState == SimState.SimRunning)
            {
                if (lastReportedValue.TryGetValue(nau, out double lastValue))
                {
                    handler(this, new ValueChangedEventArgs { NewValue = lastValue });
                }
                else
                {
                    SendNoValueError(handler);
                }
            }
            else
            {
                SendNoConnectionError(handler);
            }
        }

        /// <summary>
        /// Reports that, whilst we are connected to the server, we haven't yet received a value for this variable.
        /// </summary>
        private void SendNoValueError(EventHandler<ValueChangedEventArgs> handler)
        {
            handler(this, new ValueChangedEventArgs { NewValue = new Exception("No value yet received from simulator") });
        }

        /// <summary>
        /// Reports that a variable doesn't have a value because the simulator isn't connected.
        /// </summary>
        private void SendNoConnectionError(EventHandler<ValueChangedEventArgs> handler)
        {
            handler(this, new ValueChangedEventArgs { NewValue = new Exception("No connection to simulator") });
        }

        public void RemoveListener(IVariable variable, EventHandler<ValueChangedEventArgs> handler)
        {
            var simvar = (SimVarExpression)variable;
            var nau = simvar.NameAndUnits;

            lock (this)
            {
                var handlers = variableHandlers[nau];
                handlers.Remove(handler);
                if (handlers.Count == 0)
                {
                    if (this.simconnect != null)
                    {
                        UnsubscribeFromSimConnect(simvar);
                    }
                }
            }
        }

        private const int WM_USER_SIMCONNECT = 0x0402;

        private SimConnect simconnect;

        public void ReceiveMessage()
        {
            if (simconnect != null)
            {
                try
                {
                    simconnect.ReceiveMessage();
                } catch (Exception)
                {
                    // Just squash. Sim's probably about to exit.
                }
            }
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            ConnectNow();
        }

        private void ConnectNow()
        {
            if (reconnectTimer.Enabled)
            {
                reconnectTimer.Stop();
            }

            if (simconnect != null)
            {
                return;
            }

            try
            {
                simconnect = new SimConnect("BravoLights", HWnd, WM_USER_SIMCONNECT, null, 0);
                simconnect.OnRecvSimobjectData += Simconnect_OnRecvSimobjectData;
                simconnect.OnRecvQuit += Simconnect_OnRecvQuit;

                simconnect.OnRecvEvent += Simconnect_OnRecvEvent;
                simconnect.SubscribeToSystemEvent(RequestId.SimState, "Sim");

                simconnect.OnRecvSystemState += Simconnect_OnRecvSystemState;
                simconnect.RequestSystemState(RequestId.SimState, "Sim");

                ConfigureWASMComms();
                SendLVarRequest("CLEAR");
                SendLVarRequest("LISTLVARS");

                RequestAircraftLoaded();

                RegisterCurrentVariables();
            }
            catch (Exception)
            {
                reconnectTimer.Start();

                if (simState == SimState.SimRunning)
                {
                    RaiseSimStateChanged(SimState.SimStopped);
                }
            }
        }

        private void ConfigureWASMComms()
        {
            simconnect.MapClientDataNameToID(CDA_NAME_SIMVAR, ClientDataId.LVars);
            simconnect.CreateClientData(ClientDataId.LVars, (uint)Marshal.SizeOf<LVarData>(), SIMCONNECT_CREATE_CLIENT_DATA_FLAG.DEFAULT);

            simconnect.MapClientDataNameToID(CDA_NAME_REQUEST, ClientDataId.Request);
            simconnect.CreateClientData(ClientDataId.Request, 256, SIMCONNECT_CREATE_CLIENT_DATA_FLAG.DEFAULT);

            simconnect.MapClientDataNameToID(CDA_NAME_RESPONSE, ClientDataId.Response);
            simconnect.CreateClientData(ClientDataId.Response, 256, SIMCONNECT_CREATE_CLIENT_DATA_FLAG.DEFAULT);

            simconnect.AddToClientDataDefinition(DefineId.WASMRequestResponse, 0, RequestResponseSize, 0, 0);
            simconnect.RegisterStruct<SIMCONNECT_RECV_CLIENT_DATA, ResponseString>(DefineId.WASMRequestResponse);
            simconnect.RequestClientData(
                ClientDataId.Response,
                RequestId.WASMResponse,
                DefineId.WASMRequestResponse,
                SIMCONNECT_CLIENT_DATA_PERIOD.ON_SET,
                SIMCONNECT_CLIENT_DATA_REQUEST_FLAG.DEFAULT,
                0,
                0,
                0
            );

            simconnect.AddToClientDataDefinition(DefineId.WASMLVars, 0, LVarDataSize, 0, 0);
            simconnect.RegisterStruct<SIMCONNECT_RECV_CLIENT_DATA, LVarData>(DefineId.WASMLVars);
            simconnect.RequestClientData(
                ClientDataId.LVars,
                RequestId.WASMLVars,
                DefineId.WASMLVars,
                SIMCONNECT_CLIENT_DATA_PERIOD.ON_SET,
                SIMCONNECT_CLIENT_DATA_REQUEST_FLAG.DEFAULT,
                0,
                0,
                0
            );

            simconnect.OnRecvClientData += SimConnect_OnRecvClientData;
        }

        private void RequestAircraftLoaded()
        {
            simconnect.RequestSystemState(RequestId.AircraftLoaded, "AircraftLoaded");
        }

        private void Simconnect_OnRecvEvent(SimConnect sender, SIMCONNECT_RECV_EVENT data)
        {
            lock (this)
            {
                switch ((EventId)data.uEventID)
                {
                    case EventId.SimState:
                        var running = data.dwData == 1;
                        if (running)
                        {
                            RequestAircraftLoaded();
                        }
                        RaiseSimStateChanged(running ? SimState.SimRunning : SimState.SimStopped);
                        break;
                }
            }
        }

        private void Simconnect_OnRecvSystemState(SimConnect sender, SIMCONNECT_RECV_SYSTEM_STATE data)
        {
            lock (this)
            {
                switch ((RequestId)data.dwRequestID)
                {
                    case RequestId.SimState:
                        RaiseSimStateChanged(data.dwInteger == 1 ? SimState.SimRunning : SimState.SimStopped);
                        break;
                    case RequestId.AircraftLoaded:
                        RaiseAircraftChanged(data.szString);
                        break;
                }
            }
        }

        private void RaiseSimStateChanged(SimState state)
        {
            lock (this)
            {
                if (simState == state)
                {
                    // State is unchanged
                    return;
                }

                simState = state;

                if (simState == SimState.SimExited)
                {
                    // The sim has exited. We're likely to be exiting soon, but in
                    // case we don't, we should tidy up.

                    // Any previous values are now not valid.
                    lastReportedValue.Clear();

                    // SimConnect has gone away; presumably MSFS has exited.
                    // BetterBravoLights itself might not be exiting (if it's being run without install),
                    // but we need to clean up these registrations.
                    this.idToName.Clear();
                    this.nameToId.Clear();
                    simconnect = null;

                    // Tell all existing subscribers that we have no connection.
                    foreach (var variableHandlers in variableHandlers.Values)
                    {
                        foreach (var handler in variableHandlers)
                        {
                            SendNoConnectionError(handler);
                        }
                    }
                }

                OnSimStateChanged?.Invoke(this, new SimStateEventArgs { SimState = state });
            }
        }

        private static readonly Regex AircraftPathRegex = new("Airplanes\\\\(.*)\\\\");

        private void RaiseAircraftChanged(string aircraftPath)
        {
            if (OnAircraftLoaded != null)
            {
                // aircraftPath is something like
                // SimObjects\\Airplanes\\Asobo_C172SP_Classic\\aircraft.CFG
                // We just want 'Asobo_C172SP_Classic'
                var match = AircraftPathRegex.Match(aircraftPath);
                if (match.Success)
                {
                    OnAircraftLoaded(this, new AircraftEventArgs { Aircraft = match.Groups[1].Value });
                }
            }
        }

        // Called when MSFS quits
        private void Simconnect_OnRecvQuit(SimConnect sender, SIMCONNECT_RECV data)
        {
            RaiseSimStateChanged(SimState.SimExited);
        }

        private void RegisterCurrentVariables()
        {
            foreach (var nau in this.variableHandlers.Keys)
            {
                SubscribeToSimConnect(nau);
            }
        }

        private List<string> incomingLVars = new List<string>();
        private Dictionary<string, int> lvarIds = new();
        private Dictionary<int, string> lvarNames = new();

        private WASMReaderState readerState = WASMReaderState.Neutral;

        private void SimConnect_OnRecvClientData(SimConnect sender, SIMCONNECT_RECV_CLIENT_DATA data)
        {
            switch ((RequestId)data.dwRequestID)
            {
                case RequestId.WASMResponse:
                    {
                        var responseString = ((ResponseString)data.dwData[0]).Data;

                        switch (responseString)
                        {
                            case RESPONSE_LVAR_START:
                                readerState = WASMReaderState.ReadingLVars;
                                return;
                            case RESPONSE_LVAR_END:
                                readerState = WASMReaderState.Neutral;
                                var newLVars = incomingLVars;
                                incomingLVars = new();
                                LVarManager.Connection.UpdateLVarList(newLVars);
                                return;
                        }

                        if (readerState == WASMReaderState.ReadingLVars)
                        {
                            incomingLVars.Add(responseString);
                            return;
                        }
                    }
                    break;
                case RequestId.WASMLVars:
                    { 
                        var lvarUpdate = ((LVarData)data.dwData[0]);
                        LVarManager.Connection.UpdateLVarValues(lvarUpdate);
                    }
                    break;
            }
        }

        private void Simconnect_OnRecvSimobjectData(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA data)
        {
            lock (this)
            {
                if (idToName.TryGetValue(data.dwRequestID, out NameAndUnits nau))
                {
                    var dataContainer = data.dwData[0] as ContainerStruct?;
                    var newValue = dataContainer.Value.doubleValue;

                    var handlers = this.variableHandlers[nau];

                    if (lastReportedValue.TryGetValue(nau, out double lastValue))
                    {
                        if (newValue == lastValue)
                        {
                            // Value is unchanged
                            return;
                        }
                    }
                    lastReportedValue[nau] = newValue;

                    var e = new ValueChangedEventArgs { NewValue = newValue };
                    foreach (var handler in handlers)
                    {
                        handler(this, e);
                    }
                }
                else
                {
                    // We're receiving data we don't currently have entries for,
                    // probably from previous runs of the sim if the app has
                    // been running during multiple sim runs. Unsubscribe.
                    if (simconnect != null)
                    {
                        simconnect.ClearClientDataDefinition((DefineId)data.dwRequestID);
                    }
                }
            }
        }

        public static IntPtr HWnd { get; set; }

        public event EventHandler<AircraftEventArgs> OnAircraftLoaded;
        public event EventHandler<SimStateEventArgs> OnSimStateChanged;

        private void SendLVarRequest(string message)
        {
            var cmd = new RequestString(message);
            simconnect.SetClientData(
                ClientDataId.Request,
                DefineId.WASMRequestResponse,
                SIMCONNECT_CLIENT_DATA_SET_FLAG.DEFAULT,
                0,
                cmd
            );
        }

        void IWASMChannel.ClearSubscriptions()
        {
            SendLVarRequest($"CLEAR");
        }

        void IWASMChannel.Subscribe(short id)
        {
            SendLVarRequest($"SUBSCRIBE {id.ToString(CultureInfo.InvariantCulture)}");
        }

        void IWASMChannel.Unsubscribe(short id)
        {
            SendLVarRequest($"UNSUBSCRIBE {id.ToString(CultureInfo.InvariantCulture)}");
        }
    }

    public enum SimState
    {
        SimRunning,
        SimStopped,
        SimExited
    }

    class NameAndUnits
    {
        public string Name;
        public string Units;

        public override string ToString()
        {
            return $"{Name}, {Units}";
        }
    }

    class NameAndUnitsComparer : IEqualityComparer<NameAndUnits>
    {
        public bool Equals(NameAndUnits x, NameAndUnits y)
        {
            return x.Name == y.Name && x.Units == y.Units;
        }

        public int GetHashCode([DisallowNull] NameAndUnits obj)
        {
            return obj.Name.GetHashCode();
        }
    }

    class SimStateEventArgs: EventArgs
    {
        public SimState SimState;
    }

    class AircraftEventArgs : EventArgs
    {
        public string Aircraft;
    }

    [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Ansi, Pack = 1)]
    struct ContainerStruct
    {
        [FieldOffset(0)]
        public double doubleValue;
    }
}
