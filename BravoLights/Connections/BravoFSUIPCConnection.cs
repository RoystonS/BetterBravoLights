using System;
using System.Collections.Generic;
using System.Timers;
using BravoLights.Ast;
using FSUIPC;

namespace BravoLights.Connections
{
    class BravoFSUIPCConnection : IConnection
    {
        public readonly static BravoFSUIPCConnection Connection = new BravoFSUIPCConnection();

        private BravoFSUIPCConnection()
        {
            readTimer.Elapsed += ReadTimer_Elapsed;
            reconnectTimer.Elapsed += ReconnectTimer_Elapsed; ;
        }

        private readonly Dictionary<string, double> lastReportedValue = new Dictionary<string, double>();
        private readonly Dictionary<string, ISet<EventHandler<ValueChangedEventArgs>>> variableHandlers =
            new Dictionary<string, ISet<EventHandler<ValueChangedEventArgs>>>();

        // Reading LVARs the way we do right now is a little expensive, so just 10Hz.
        private Timer readTimer = new Timer { AutoReset = false, Interval = 100 };
        private Timer reconnectTimer = new Timer { AutoReset = false, Interval = 30000 };

        private void ReadTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                foreach (var variableInfo in variableHandlers)
                {
                    var lvarName = variableInfo.Key;
                    var handlers = variableInfo.Value;

                    try
                    {
                        double oldValue;
                        var newValue = FSUIPCConnection.ReadLVar(lvarName);

                        if (lastReportedValue.TryGetValue(lvarName, out oldValue))
                        {
                            if (oldValue == newValue)
                            {
                                // Value is unchanged
                                continue;
                            }
                        }

                        lastReportedValue[lvarName] = newValue;

                        var ea = new ValueChangedEventArgs { NewValue = newValue };
                        foreach (var handler in handlers)
                        {
                            handler(this, ea);
                        }
                    } catch (Exception)
                    {
                    }
                }
            }
            finally
            {
                readTimer.Start();
            }
        }

        public void AddListener(IVariable variable, EventHandler<ValueChangedEventArgs> handler)
        {
            var lvar = (FSUIPCLvarExpression)variable;
            var name = lvar.Name;

            if (variableHandlers.Count == 0)
            {
                StartFSUIPC();
            }
    
            ISet<EventHandler<ValueChangedEventArgs>> handlers;
            if (!variableHandlers.TryGetValue(name, out handlers))
            {
                handlers = new HashSet<EventHandler<ValueChangedEventArgs>>();
                variableHandlers.Add(name, handlers);
            }

            handlers.Add(handler);

            SendLastValue(variable, this, handler);
        }

        public void SendLastValue(IVariable variable, object sender, EventHandler<ValueChangedEventArgs> handler)
        {
            var simvar = (FSUIPCLvarExpression)variable;
            var name = simvar.Name;
            double lastValue;
            if (lastReportedValue.TryGetValue(name, out lastValue))
            {
                handler(sender, new ValueChangedEventArgs { NewValue = lastValue });
            }
        }

        public void RemoveListener(IVariable variable, EventHandler<ValueChangedEventArgs> handler)
        {
            var lvar = (FSUIPCLvarExpression)variable;
            var name = lvar.Name;

            var handlers = this.variableHandlers[name];
            handlers.Remove(handler);
            if (handlers.Count == 0)
            {
                variableHandlers.Remove(name);
            }
            if (variableHandlers.Count == 0)
            {
                StopFSUIPC();
            }
        }

        private void StartFSUIPC()
        {
            try
            {
                FSUIPCConnection.Open(FlightSim.MSFS);
                readTimer.Start();
            } catch
            {
                // Sim not running?
                // Try again soon
                reconnectTimer.Start();
            }
        }

        private void StopFSUIPC()
        {
            readTimer.Stop();
            FSUIPCConnection.Close();
        }

        private void ReconnectTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (variableHandlers.Count > 0)
            {
                StartFSUIPC();
            }
        }
    }
}
