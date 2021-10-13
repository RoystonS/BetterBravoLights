using System.Collections.Generic;
using System.Collections.ObjectModel;
using BravoLights.Common;

namespace BravoLights.UI
{
    public class ExpressionAndVariablesViewModel : ViewModelBase
    {
        private ObservableCollection<VariableState> variables = new();
        public ObservableCollection<VariableState> Variables
        {
            get { return variables; }
            private set
            {
                SetProperty(ref variables, value);
            }
        }

        private string expressionText = "";
        public string ExpressionText
        {
            get { return expressionText; }
            private set
            {
                SetProperty(ref expressionText, value);
            }
        }

        private bool expressionErrored = false;
        public bool ExpressionErrored
        {
            get { return expressionErrored; }
            private set
            {
                SetProperty(ref expressionErrored, value);
            }
        }

        private ISet<IVariable> monitoredVariables;

        public void Monitor(LightExpression lightExpression)
        {
            if (monitoredVariables != null)
            {
                foreach (var variable in monitoredVariables)
                {
                    variable.ValueChanged -= Variable_ValueChanged;
                }
            }
            monitoredVariables = null;

            Variables = null;
            ExpressionText = "";
            ExpressionErrored = false;

            if (lightExpression != null)
            {
                ExpressionText = lightExpression.Expression.ToString();
                ExpressionErrored = lightExpression.Expression.ErrorText != null;

                var variables = new ObservableCollection<VariableState>();
                Variables = variables;

                monitoredVariables = new HashSet<IVariable>(lightExpression.Expression.Variables);
                foreach (var variable in monitoredVariables)
                {
                    var variableState = new VariableState { Name = variable.Identifier };
                    variables.Add(variableState);

                    variable.ValueChanged += Variable_ValueChanged;
                }
            }
        }

        private void Variable_ValueChanged(object sender, ValueChangedEventArgs e)
        {
            var variable = sender as IVariable;

            foreach (var variableState in Variables)
            {
                if (variableState.Name == variable.Identifier)
                {
                    variableState.Value = e.NewValue;
                }
            }
        }
    }
}
