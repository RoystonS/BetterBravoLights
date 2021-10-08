using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace BravoLights.UI
{
    /// <summary>
    /// Interaction logic for LightsWindow.xaml
    /// </summary>
    public partial class LightsWindow : Window
    {
        public LightsWindow()
        {
            InitializeComponent();
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            Title = $"{ProgramInfo.ProductNameAndVersion} - Lights Monitor";
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
                    ExpressionAndVariablesViewModel = eavVM
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

        private string monitoredLight = "";

        private void Checkbox_Checked(object sender, RoutedEventArgs e)
        {            
            monitoredLight = ((Control)e.OriginalSource).Tag as string;

            UpdateMonitor();
        }

        private readonly ExpressionAndVariablesViewModel eavVM = new();

        private void UpdateMonitor()
        {
            LightExpression lightExpression = null;

            if (monitoredLight != null && viewModel != null)
            {
                viewModel.LightExpressions.TryGetValue(monitoredLight, out lightExpression);
            }
            eavVM.Monitor(lightExpression);
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

    class CombinedDataContext
    {
        private MainViewModel mainState;
        public MainViewModel MainState
        {
            get { return mainState; }
            set { mainState = value; }
        }

        private ExpressionAndVariablesViewModel eavVM;

        public ExpressionAndVariablesViewModel ExpressionAndVariablesViewModel
        {
            get { return eavVM; }
            set { eavVM = value; }
        }
    }

    public class VariableState : ViewModelBase
    {
        public VariableState()
        {
            ValueText = "No value received yet";
            IsError = true;
        }

        public string Name { get; set; }

        private string val;
        public string ValueText
        {
            get { return val; }
            private set
            {
                SetProperty(ref val, value);
            }
        }

        private bool isError;
        public bool IsError
        {
            get { return isError; }
            private set
            {
                SetProperty(ref isError, value);
            }
        }

        private object valueObject;
        public object Value
        {
            get { return valueObject; }
            set
            {
                SetProperty(ref valueObject, value);

                var exception = value as Exception;
             
                IsError = exception != null;
                
                if (exception != null)
                {
                    ValueText = exception.Message;
                }
                else
                {
                    ValueText = value.ToString();
                }
            }
        }
    }
}
