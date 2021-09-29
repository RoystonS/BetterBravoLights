using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace BravoLights.UI
{
    /// <summary>
    /// Interaction logic for LightsWindow.xaml
    /// </summary>
    public partial class LightsWindow : Window
    {
        private readonly MonitoringState monitoringState = new MonitoringState();

        public LightsWindow()
        {
            InitializeComponent();
        }

        private MainViewModel viewModel;

        public MainViewModel ViewModel
        {
            get
            {
                return (MainViewModel)DataContext;
            }
            set
            {
                viewModel = value;
                viewModel.PropertyChanged += ViewModel_PropertyChanged;
                DataContext = new CombinedDataContext
                {
                    MainState = value,
                    MonitoringState = monitoringState
                };
            }
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "LightExpressions")
            {
                UpdateMonitor();
            }
        }

        private string monitoredLight;
        private LightExpression monitoredLightExpression;
        private Dictionary<string, double> monitoredVariableValues = new Dictionary<string, double>();

        private void Checkbox_Checked(object sender, RoutedEventArgs e)
        {            
            monitoredLight = ((Control)e.OriginalSource).Tag as string;

            UpdateMonitor();
        }

        private void UpdateMonitor()
        {
            LightExpression lightExpression = null;

            if (monitoredLight != null)
            {
                viewModel.LightExpressions.TryGetValue(monitoredLight, out lightExpression);
            }

            if (monitoredLightExpression != lightExpression || !this.IsVisible)
            {
                if (monitoredLightExpression != null)
                {
                    foreach (var variable in monitoredLightExpression.Expression.Variables)
                    {
                        variable.ValueChanged -= Variable_ValueChanged;
                    }
                }

                if (!IsVisible)
                {
                    return;
                }

                monitoringState.Text = "";
                monitoredLightExpression = lightExpression;
                monitoredVariableValues.Clear();

                UpdateMonitorText();

                if (lightExpression != null)
                {
                    foreach (var variable in lightExpression.Expression.Variables)
                    {
                        variable.ValueChanged += Variable_ValueChanged;
                    }
                }
            }
        }

        private void Variable_ValueChanged(object sender, ValueChangedEventArgs e)
        {
            var variable = sender as IVariable;
            monitoredVariableValues[variable.Name] = (double)e.NewValue;

            UpdateMonitorText();
        }

        private void UpdateMonitorText() {
            var text = new StringBuilder();
            text.AppendLine("Expression:");
            text.AppendLine(monitoredLightExpression.Expression.ToString());

            text.AppendLine();
            text.AppendLine("Values:");

            foreach (var kvp in monitoredVariableValues)
            {
                text.AppendFormat("{0} = {1}", kvp.Key, kvp.Value);
                text.AppendLine();
            }
            monitoringState.Text = text.ToString();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            // Hide instead of close
            e.Cancel = true;
            Hide();

            // Unsubscribe whilst invisible
            UpdateMonitor();
        }
    }

    class MonitoringState : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string text = "";

        public string Text
        {
            get { return text; }
            set
            {
                text = value;
                RaisePropertyChanged("Text");
            }
        }

        private void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    class CombinedDataContext
    {
        private MainViewModel mainState;
        public MainViewModel MainState
        {
            get { return mainState; }
            set { mainState = value; }
        }

        private MonitoringState monitoringState;

        public MonitoringState MonitoringState
        {
            get { return monitoringState; }
            set { monitoringState = value; }
        }
    }
}
