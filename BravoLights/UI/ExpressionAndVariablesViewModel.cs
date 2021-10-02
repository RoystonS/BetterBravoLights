using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;


namespace BravoLights.UI
{
    public class ExpressionAndVariablesViewModel : ViewModelBase
    {
        private ObservableCollection<VariableState> variables;
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

        private LightExpression monitoredExpression;

        public void Monitor(LightExpression lightExpression)
        {
            if (monitoredExpression != null)
            {
                foreach (var variable in monitoredExpression.Expression.Variables)
                {
                    variable.ValueChanged -= Variable_ValueChanged;
                }
            }

            Variables = null;
            ExpressionText = "";
            ExpressionErrored = false;

            monitoredExpression = lightExpression;

            if (lightExpression != null)
            {
                ExpressionText = lightExpression.Expression.ToString();
                ExpressionErrored = lightExpression.Expression.ErrorText != null;

                var variables = new ObservableCollection<VariableState>();
                Variables = variables;

                var expressionVariablesDeduped = new HashSet<IVariable>(lightExpression.Expression.Variables);
                foreach (var variable in expressionVariablesDeduped)
                {
                    var variableState = new VariableState { Name = variable.Identifier, Value = "Waiting for value...", IsError = true };
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
                    var exception = e.NewValue as Exception;
                    variableState.IsError = exception != null;
                    if (exception != null)
                    {
                        variableState.Value = exception.Message;
                    }
                    else
                    {
                        variableState.Value = e.NewValue.ToString();
                    }
                }
            }
        }
    }
}
