using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Timers;
using BravoLights.Ast;
using BravoLights.Common;
using Microsoft.FlightSimulator.SimConnect;

namespace BravoLights.Connections
{
    enum Ids : uint
    {
        SimState = 1,
        AircraftLoaded = 3,

        DynamicStart = 1000,
    }

    class SimConnectConnection : IConnection
    {
        public static SimConnectConnection Connection = new();

        private readonly Timer reconnectTimer = new();
        private SimState simState = SimState.SimStopped;

        private SimConnectConnection()
        {
            reconnectTimer.AutoReset = false;
            reconnectTimer.Interval = new TimeSpan(0, 0, 30).TotalMilliseconds;
            reconnectTimer.Elapsed += Timer_Elapsed;
        }
        public void Start()
        {
            ConnectNow();
        }

        private uint nextVariableId = (uint)Ids.DynamicStart;

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

            simconnect.AddToDataDefinition((Ids)id, nameAndUnits.Name, nameAndUnits.Units, SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
            simconnect.RegisterDataDefineStruct<ContainerStruct>((Ids)id);
            simconnect.RequestDataOnSimObject((Ids)id, (Ids)id, 0, SIMCONNECT_PERIOD.SIM_FRAME, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);
        }

        private void UnsubscribeFromSimConnect(SimVarExpression simvar)
        {
            var name = simvar.NameAndUnits;
            uint id = 0;
            try
            {
                id = nameToId[name];
                simconnect.ClearClientDataDefinition((Ids)id);
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
                simconnect.SubscribeToSystemEvent(Ids.SimState, "Sim");

                simconnect.OnRecvSystemState += Simconnect_OnRecvSystemState;
                simconnect.RequestSystemState(Ids.SimState, "Sim");

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

        private void RequestAircraftLoaded()
        {
            simconnect.RequestSystemState(Ids.AircraftLoaded, "AircraftLoaded");
        }

        private void Simconnect_OnRecvEvent(SimConnect sender, SIMCONNECT_RECV_EVENT data)
        {
            lock (this)
            {
                switch ((Ids)data.uEventID)
                {
                    case Ids.SimState:
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
                switch ((Ids)data.dwRequestID)
                {
                    case Ids.SimState:
                        RaiseSimStateChanged(data.dwInteger == 1 ? SimState.SimRunning : SimState.SimStopped);
                        break;
                    case Ids.AircraftLoaded:
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
                        simconnect.ClearClientDataDefinition((Ids)data.dwRequestID);
                    }
                }
            }
        }

        public static IntPtr HWnd { get; set; }

        public event EventHandler<AircraftEventArgs> OnAircraftLoaded;
        public event EventHandler<SimStateEventArgs> OnSimStateChanged;
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
