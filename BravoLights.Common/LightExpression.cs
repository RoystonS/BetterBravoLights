using System;
using BravoLights.Common.Ast;

namespace BravoLights.Common
{
    public class LightExpression
    {
        public LightExpression(string lightName, IAstNode expression, bool errorIfNotBooleanExpression)
        {
            LightName = lightName;
            Expression = expression;

            if (errorIfNotBooleanExpression && expression.ValueType != NodeDataType.Boolean)
            {
                Expression = new ErrorNode($"A boolean expression is needed to drive a light, not a numeric one.");
            }
        }

        public readonly string LightName;
        public readonly IAstNode Expression;

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
