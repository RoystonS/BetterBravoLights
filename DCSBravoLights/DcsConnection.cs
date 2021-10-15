using System;
using BravoLights.Common;

namespace DCSBravoLights
{
    class DcsConnection : IConnection
    {
        public static readonly DcsConnection Connection = new();

        public DcsVariablesManager DcsVariablesManager;

        public void AddListener(IVariable variable, EventHandler<ValueChangedEventArgs> handler)
        {
            var dcsVariable = (DcsVariableExpression)variable;
            var variableName = dcsVariable.VariableName;

            DcsVariablesManager.AddHandler(variableName, handler);
        }

        public void RemoveListener(IVariable variable, EventHandler<ValueChangedEventArgs> handler)
        {
            var dcsVariable = (DcsVariableExpression)variable;
            var variableName = dcsVariable.VariableName;

            DcsVariablesManager.RemoveHandler(variableName, handler);
        }
    }
}
