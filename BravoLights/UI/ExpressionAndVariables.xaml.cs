using System.Windows;
using System.Windows.Controls;

namespace BravoLights.UI
{
    /// <summary>
    /// Interaction logic for ExpressionAndVariables.xaml
    /// </summary>
    public partial class ExpressionAndVariables : UserControl
    {
        public ExpressionAndVariables()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register("ViewModel", typeof(ExpressionAndVariablesViewModel), typeof(ExpressionAndVariables));
        public ExpressionAndVariablesViewModel ViewModel
        {
            get { return (ExpressionAndVariablesViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }
    }
}
