﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BravoLights.Ast;
using BravoLights.Common;
using NLog;

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

        private readonly ILogger logger = LogManager.GetCurrentClassLogger();
        private readonly Dictionary<string, short> lvarIds = new();
        private readonly Dictionary<short, string> lvarNames = new();
        private readonly Dictionary<string, double> lvarValues = new();

        private readonly Dictionary<string, EventHandler<ValueChangedEventArgs>> handlers = new();

        public void AddListener(IVariable variable, EventHandler<ValueChangedEventArgs> handler)
        {
            var lvar = (LvarExpression)variable;
            var name = lvar.LVarName;

            logger.Trace("AddListener {0}", variable.Identifier);

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

            logger.Trace("RemoveListener {0}", variable.Identifier);

            lock (this)
            {
                handlers.TryGetValue(name, out var existingHandlersForDefinition);
                var newListeners = (EventHandler<ValueChangedEventArgs>)Delegate.Remove(existingHandlersForDefinition, handler);

                if (newListeners == null)
                {
                    handlers.Remove(name);
                    // No more subscriptions for this variable
                    if (lvarIds.TryGetValue(name, out var id))
                    {
                        wasmChannel.Unsubscribe(id);
                    }
                }
                else
                {
                    handlers[name] = newListeners;
                }

                SendLastValue(name, handler);
            }
        }

        private void SendLastValue(string name, EventHandler<ValueChangedEventArgs> handler)
        {
            logger.Trace("SendLastValue:{0}", name);

            if (DisableLVars)
            {
                handler(this, new ValueChangedEventArgs { NewValue = new Exception("The LVars module is not installed") });
                return;
            }

            if (wasmChannel.SimState == SimState.SimExited)
            {
                VariableHandlerUtils.SendNoConnectionError(this, handler);
                return;
            }

            if (lvarValues.TryGetValue(name, out var lastValue))
            {
                logger.Trace("SendLastValue:{0}:{1}", name, lastValue);
                handler(this, new ValueChangedEventArgs { NewValue = lastValue });
                return;
            }

            // We have no value for this lvar. Is it because the LVar doesn't exist, or just
            // because it's not being used for _this_ aircraft?
            if (lvarIds.ContainsKey(name))
            {
                logger.Trace("SendLastValue:{0}:NoValue", name);
                VariableHandlerUtils.SendNoValueError(this, handler);
            }
            else
            {
                logger.Trace("SendLastValue:{0}:NoSuchVar", name);
                SendNoSuchLVarError(handler);
            }
        }

        private IWASMChannel wasmChannel;

        private bool disableLVars;

        /// <summary>
        /// Gets or sets a value that disables LVar support. This is typically set if we know that the LVar module is missing or the wrong version.
        /// </summary>
        public bool DisableLVars
        {
            get { return disableLVars; }
            internal set
            {
                disableLVars = value;
                logger.Debug("DisableLVars = {0}", value);
            }
        }

        public void SetWASMChannel(IWASMChannel wasmChannel)
        {
            this.wasmChannel = wasmChannel;
        }

        private void SendNoSuchLVarError(EventHandler<ValueChangedEventArgs> handler)
        {
            handler(this, new ValueChangedEventArgs { NewValue = new Exception("LVar does not exist yet; this aircraft may not support it") });
        }

        private void SendAllValues()
        {
            lock (this)
            {
                foreach (var kvp in this.handlers)
                {
                    var lvarName = kvp.Key;
                    var handler = kvp.Value;
                    SendLastValue(lvarName, handler);
                }
            }
        }

        /// <summary>
        /// Informs the LVar manager that the simulator list of LVars has changed.
        /// </summary>
        /// <remarks>
        /// The list _usually_ grows but if the simulator is restarted without restarting BBL, it will shrink.
        /// </remarks>
        public void UpdateLVarList(List<string> newLVars)
        {
            // During one run of the simulator, lvar ids will not disappear, but
            // between runs of the simulator, all bets are off.
            // So it's just safest for us to reset and resubscribe whenever lvars change.

            lock (this)
            {
                wasmChannel.ClearSubscriptions();

                var oldCount = lvarIds.Count;
                lvarIds.Clear();
                lvarNames.Clear();

                logger.Debug($"Incoming lvars: {newLVars.Count}");
                for (short id = 0; id < newLVars.Count; id++)
                {
                    var name = newLVars[id];

                    if (id >= oldCount)
                    {
                        logger.Debug($"Received new lvar: {name}");
                    }

                    if (!lvarIds.ContainsKey(name))
                    {
                        lvarIds[name] = id;
                        lvarNames[id] = name;

                        if (handlers.ContainsKey(name))
                        {
                            wasmChannel.Subscribe(id);
                        }
                    }
                }

                OnLVarListChanged?.Invoke(this, EventArgs.Empty);

                SendAllValues();
            }
        }

        public IList<string> LVarList
        {
            get
            {
                lock (this)
                {
                    return lvarIds.Keys.ToList();
                }
            }
        }

        public event EventHandler OnLVarListChanged;

        public void UpdateLVarValues(ILVarData data)
        {
            logger.Trace("UpdateLVarValues");

            lock (this)
            {
                for (var i = 0; i < data.ValueCount; i++)
                {
                    var id = data.Ids[i];
                    var value = data.Values[i];
                    try
                    {
                        var name = lvarNames[id];

                        lvarValues[name] = value;

                        if (handlers.TryGetValue(name, out var handler))
                        {
                            SendLastValue(name, handler);
                        }
                    }
                    catch (Exception)
                    {
                        // When starting up with a running sim we might get lvar updates
                        // before we have a full list of lvars, so we might not be able
                        // to find them
                        logger.Warn("Avoided race condition in UpdateLVarValues for {0}:{1}", data.Ids[i], data.Values[i]);
                    }
                }
            }
        }
    }
}
