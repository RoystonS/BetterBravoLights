using System;
using System.Collections.Generic;
using BravoLights.Connections;

namespace BravoLights.Ast
{
    abstract class VariableBase : IVariable
    {
        public string ErrorText { get { return null; } }

        public IEnumerable<IVariable> Variables
        {
            get { yield return this; }
        }

        public abstract IConnection Connection { get; }
        public abstract string Identifier { get; }

        private EventHandler<ValueChangedEventArgs> handlers;

        public event EventHandler<ValueChangedEventArgs> ValueChanged
        {
            add
            {
                var subscribe = handlers == null;

                // It's important that we add the listener before subscribing to children
                // because the subscription may fire immediately
                handlers += value;
                if (subscribe)
                {
                    Connection.AddListener(this, OnVariableChanged);
                } else
                {
                    Connection.SendLastValue(this, this, value);
                }
            }
            remove
            {
                handlers -= value;
                if (handlers == null)
                {
                    Connection.RemoveListener(this, OnVariableChanged);
                }
            }
        }

        private void OnVariableChanged(object sender, ValueChangedEventArgs e)
        {
            if (handlers != null)
            {
                handlers(this, e);
            }
        }

        public bool Equals(IVariable other)
        {
            return Identifier.Equals(other.Identifier);
        }
        public override int GetHashCode()
        {
            return Identifier.GetHashCode();
        }
    }
}
