using System;
using BravoLights.Common;
using BravoLights.Common.Ast;

namespace BravoLights
{
    public class LightExpression
    {
        public string LightName;
        public IAstNode Expression;

        private EventHandler<ValueChangedEventArgs> handlers;

        public event EventHandler<ValueChangedEventArgs> ValueChanged
        {
            add
            {
                var send = handlers == null;
                handlers += value;
                if (send)
                {
                    Expression.ValueChanged += Expression_ValueChanged;
                }
            }
            remove
            {
                handlers -= value;
                if (handlers == null)
                {
                    Expression.ValueChanged -= Expression_ValueChanged;
                }
            }
        }

        private void Expression_ValueChanged(object sender, ValueChangedEventArgs e)
        {
            handlers?.Invoke(this, e);
        }
    }
}
