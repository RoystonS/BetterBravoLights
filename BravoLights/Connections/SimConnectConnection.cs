using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Timers;
using BravoLights.Ast;
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
        public static SimConnectConnection Connection = new SimConnectConnection();

        private readonly Timer reconnectTimer = new Timer();
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

        private readonly Dictionary<uint, NameAndUnits> idToName = new Dictionary<uint, NameAndUnits>();
        private readonly Dictionary<NameAndUnits, uint> nameToId = new Dictionary<NameAndUnits, uint>(new NameAndUnitsComparer());
        private readonly Dictionary<NameAndUnits, ISet<EventHandler<ValueChangedEventArgs>>> variableHandlers =
            new Dictionary<NameAndUnits, ISet<EventHandler<ValueChangedEventArgs>>>(new NameAndUnitsComparer());
        private readonly Dictionary<NameAndUnits, double> lastReportedValue = new Dictionary<NameAndUnits, double>(new NameAndUnitsComparer());

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

        public void AddListener(IVariable variable, EventHandler<ValueChangedEventArgs> handler)
        {
            var simvar = (SimVarExpression)variable;
            var nau = simvar.NameAndUnits;

            ISet<EventHandler<ValueChangedEventArgs>> handlers;
            if (!this.variableHandlers.TryGetValue(nau, out handlers))
            {
                handlers = new HashSet<EventHandler<ValueChangedEventArgs>>();
                variableHandlers.Add(nau, handlers);
            }

            if (handlers.Count == 0)
            {
                if (this.simconnect != null)
                {
                    SubscribeToSimConnect(nau);
                } else if (!reconnectTimer.Enabled)
                {
                    ConnectNow();
                }
            }

            handlers.Add(handler);

            SendLastValue(variable, this, handler);
        }

        public void SendLastValue(IVariable variable, object sender, EventHandler<ValueChangedEventArgs> handler)
        {
            var simvar = (SimVarExpression)variable;
            var nau = simvar.NameAndUnits;
            double lastValue;
            if (simState == SimState.SimRunning)
            {
                if (lastReportedValue.TryGetValue(nau, out lastValue))
                {
                    handler(sender, new ValueChangedEventArgs { NewValue = lastValue });
                }
                else
                {
                    handler(sender, new ValueChangedEventArgs { NewValue = new Exception("No value yet received from simulator") });
                }
            }
            else
            {
                handler(sender, new ValueChangedEventArgs { NewValue = new Exception("No connection to simulator") });
            }
        }

        public void RemoveListener(IVariable variable, EventHandler<ValueChangedEventArgs> handler)
        {
            var simvar = (SimVarExpression)variable;
            var nau = simvar.NameAndUnits;

            var handlers = this.variableHandlers[nau];
            handlers.Remove(handler);
            if (handlers.Count == 0)
            {
                if (this.simconnect != null)
                {
                    UnsubscribeFromSimConnect(simvar);
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
                RaiseSimStateChanged(SimState.SimStopped);
            }
        }

        private void RequestAircraftLoaded()
        {
            simconnect.RequestSystemState(Ids.AircraftLoaded, "AircraftLoaded");
        }

        private void Simconnect_OnRecvEvent(SimConnect sender, SIMCONNECT_RECV_EVENT data)
        {
            switch ((Ids)data.uEventID)
            {
                case Ids.SimState:
                    var running = data.dwData == 1;
                    if (running) {
                        RequestAircraftLoaded();
                    }
                    RaiseSimStateChanged(running ? SimState.SimRunning: SimState.SimStopped);
                    break;
            }
        }

        private void Simconnect_OnRecvSystemState(SimConnect sender, SIMCONNECT_RECV_SYSTEM_STATE data)
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

        private void RaiseSimStateChanged(SimState state)
        {
            simState = state;

            if (OnSimStateChanged != null)
            {
                OnSimStateChanged(this, new SimStateEventArgs { SimState = state });
            }
        }

        private static readonly Regex AircraftPathRegex = new Regex("Airplanes\\\\(.*)\\\\");

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

            simconnect = null;
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
            NameAndUnits nau;

            if (idToName.TryGetValue(data.dwRequestID, out nau))
            {
                var dataContainer = data.dwData[0] as ContainerStruct?;
                var newValue = dataContainer.Value.doubleValue;

                var handlers = this.variableHandlers[nau];

                double lastValue;
                if (lastReportedValue.TryGetValue(nau, out lastValue))
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
