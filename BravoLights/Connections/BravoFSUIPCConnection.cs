using System;
using System.Collections.Generic;
using System.Timers;
using BravoLights.Ast;
using BravoLights.Common;
using FSUIPC;

namespace BravoLights.Connections
{
    class BravoFSUIPCConnection : IConnection
    {
        public readonly static BravoFSUIPCConnection Connection = new();

        private BravoFSUIPCConnection()
        {
            readTimer.Elapsed += ReadTimer_Elapsed;
            reconnectTimer.Elapsed += ReconnectTimer_Elapsed; ;
        }

        private bool connectedToFSUIPC = false;
        private readonly Dictionary<string, double> lastReportedValue = new();
        private readonly Dictionary<string, ISet<EventHandler<ValueChangedEventArgs>>> variableHandlers =
            new();

        // Reading LVARs the way we do right now is a little expensive, so just 10Hz.
        private readonly Timer readTimer = new() { AutoReset = false, Interval = 100 };
        private readonly Timer reconnectTimer = new() { AutoReset = false, Interval = 30000 };

        private void ReadTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                lock (this)
                {
                    foreach (var variableInfo in variableHandlers)
                    {
                        var lvarName = variableInfo.Key;
                        var handlers = variableInfo.Value;

                        try
                        {
                            var newValue = FSUIPCConnection.ReadLVar(lvarName);

                            if (lastReportedValue.TryGetValue(lvarName, out double oldValue))
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
                        }
                        catch (Exception)
                        {
                        }
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
            var name = lvar.Identifier;

            if (variableHandlers.Count == 0)
            {
                StartFSUIPC();
            }

            lock (this)
            {
                if (!variableHandlers.TryGetValue(name, out ISet<EventHandler<ValueChangedEventArgs>> handlers))
                {
                    handlers = new HashSet<EventHandler<ValueChangedEventArgs>>();
                    variableHandlers.Add(name, handlers);
                }

                handlers.Add(handler);

                SendLastValue(variable, this, handler);
            }
        }

        private void SendLastValue(IVariable variable, object sender, EventHandler<ValueChangedEventArgs> handler)
        {
            var simvar = (FSUIPCLvarExpression)variable;
            var name = simvar.Identifier;
            if (connectedToFSUIPC)
            {
                if (lastReportedValue.TryGetValue(name, out double lastValue))
                {
                    handler(sender, new ValueChangedEventArgs { NewValue = lastValue });
                }
            }
            else
            {
                handler(sender, new ValueChangedEventArgs { NewValue = new Exception("Could not connect to FSUIPC, which is needed for LVAR support") });
            }
        }

        public void RemoveListener(IVariable variable, EventHandler<ValueChangedEventArgs> handler)
        {
            var lvar = (FSUIPCLvarExpression)variable;
            var name = lvar.Identifier;

            lock (this)
            {
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
        }

        private void StartFSUIPC()
        {
            try
            {
                FSUIPCConnection.Open(FlightSim.MSFS);
                readTimer.Start();
                connectedToFSUIPC = true;
            } catch
            {
                // Sim not running?
                // Try again soon
                reconnectTimer.Start();
                connectedToFSUIPC = false;
            }
        }

        private void StopFSUIPC()
        {
            connectedToFSUIPC = false;

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
