using System;
using System.Collections.Generic;
using System.Diagnostics;
using BravoLights.Ast;
using BravoLights.Common;

namespace BravoLights.Connections
{
    public interface ILVarData
    {
        short ValueCount { get; }

        short[] Ids { get; }

        double[] Values { get; }
    }

    public class LVarManager : IConnection
    {
        public readonly static LVarManager Connection = new();

        private readonly Dictionary<string, short> lvarIds = new();
        private readonly Dictionary<short, string> lvarNames = new();
        private readonly Dictionary<string, double> lvarValues = new();

        private readonly Dictionary<string, EventHandler<ValueChangedEventArgs>> handlers = new();

        public void AddListener(IVariable variable, EventHandler<ValueChangedEventArgs> handler)
        {
            var lvar = (LvarExpression)variable;
            var name = lvar.LVarName;

            lock (this)
            {
                handlers.TryGetValue(name, out var existingHandlersForDefinition);
                var newListeners = (EventHandler<ValueChangedEventArgs>)Delegate.Combine(existingHandlersForDefinition, handler);
                handlers[name] = newListeners;

                if (existingHandlersForDefinition == null)
                {
                    // First subscription for this variable
                    if (lvarIds.TryGetValue(name, out var id))
                    {
                        wasmChannel.Subscribe(id);
                    }
                }

                SendLastValue(name, handler);
            }
        }

        public void RemoveListener(IVariable variable, EventHandler<ValueChangedEventArgs> handler)
        {
            var lvar = (LvarExpression)variable;
            var name = lvar.LVarName;

            lock (this)
            {
                handlers.TryGetValue(name, out var existingHandlersForDefinition);
                var newListeners = (EventHandler<ValueChangedEventArgs>)Delegate.Remove(existingHandlersForDefinition, handler);
                handlers[name] = newListeners;

                if (newListeners == null)
                {
                    // No more subscriptions for this variable
                    if (lvarIds.TryGetValue(name, out var id))
                    {
                        wasmChannel.Unsubscribe(id);
                    }
                }

                SendLastValue(name, handler);
            }
        }

        private void SendLastValue(string name, EventHandler<ValueChangedEventArgs> handler)
        {
            if (lvarValues.TryGetValue(name, out var lastValue))
            {
                handler(this, new ValueChangedEventArgs { NewValue = lastValue });
                return;
            }

            // We have no value for this lvar. Is it because the LVar doesn't exist, or just
            // because it's not being used for _this_ aircraft?
            if (lvarIds.ContainsKey(name))
            {
                SendNoValueError(handler);
            } else
            {
                SendNoSuchLVarError(handler);
            }
        }

        private IWASMChannel wasmChannel;

        public void SetWASMChannel(IWASMChannel wasmChannel)
        {
            this.wasmChannel = wasmChannel;
        }

        private void SendNoValueError(EventHandler<ValueChangedEventArgs> handler)
        {
            handler(this, new ValueChangedEventArgs { NewValue = new Exception("No value yet received from simulator") });
        }

        private void SendNoSuchLVarError(EventHandler<ValueChangedEventArgs> handler)
        {
            handler(this, new ValueChangedEventArgs { NewValue = new Exception("LVar does not exist") });
        }

        /// <summary>
        /// Informs the LVar manager that the simulator list of LVars has changed.
        /// </summary>
        public void UpdateLVarList(List<string> lvars)
        {
            // During one run of the simulator, lvar ids will not disappear, but
            // between runs of the simulator, all bets are off.
            // So it's just safest for us to reset and resubscribe whenever lvars change.

            lock (this)
            {
                wasmChannel.ClearSubscriptions();

                lvarIds.Clear();
                lvarNames.Clear();

                Debug.WriteLine($"Incoming lvars: {lvars.Count}");
                for (short id = 0; id < lvars.Count; id++)
                {
                    var name = lvars[id];

                    if (!lvarIds.ContainsKey(name))
                    {
                        lvarIds[name] = id;
                        lvarNames[id] = name;

                        if (handlers.ContainsKey(name))
                        {
                            wasmChannel.Subscribe(id);
                            // We don't need to send a last value here: we will have already sent them
                            // a "this lvar doesn't exist" value
                        }

                        /*
                        switch (name)
                        {
                            case "Generic_Master_Caution_Active":
                            case "Generic_Master_Warning_Active":
                            case "XMLVAR_Autopilot_1_Status":
                            case "HANDLING_ElevatorTrim":
                            case "HUD_AP_SELECTED_SPEED":
                            case "HUD_AP_SELECTED_ALTITUDE":
                            case "DEICE_Pitot_1":
                            case "DEICE_Pitot_2":
                            case "AS3000_Brightness":
                            case "XMLVAR_SyntheticVision_On":
                            case "XMLVAR_SyntheticVision_Off":
                                Debug.WriteLine("Subscribing to {0} {1}", name, id);
                                SubscribeToLVar(id);
                                break;
                        }
                        */
                    }
                }
            }
        }

        public void UpdateLVarValues(ILVarData data)
        {
            lock (this)
            {
                for (var i = 0; i < data.ValueCount; i++)
                {
                    var id = data.Ids[i];
                    var value = data.Values[i];
                    var name = lvarNames[id];

                    Debug.WriteLine("LVAR VALUE UPDATE {0} {1} {2}", id, name, value);

                    lvarValues[name] = value;

                    if (handlers.TryGetValue(name, out var handler))
                    {
                        SendLastValue(name, handler);
                    }
                }
            }
        }
    }
}
