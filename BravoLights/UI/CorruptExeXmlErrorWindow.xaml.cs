using System.Diagnostics;
using System.IO;
using System.Windows;

namespace BravoLights.UI
{
    /// <summary>
    /// Interaction logic for CorruptExeXmlErrorWindow.xaml
    /// </summary>
    public partial class CorruptExeXmlErrorWindow : Window
    {
        public CorruptExeXmlErrorWindow()
        {
            InitializeComponent();
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            e.Handled = true;
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        }
        private void ShowExeXmlLocation(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            e.Handled = true;
            Process.Start(new ProcessStartInfo(Path.GetDirectoryName(XmlFilename)) { UseShellExecute = true });
        }

        public static readonly DependencyProperty XmlFilenameProperty = DependencyProperty.Register("XmlFilename", typeof(string), typeof(CorruptExeXmlErrorWindow));
        public string XmlFilename
        {
            get { return (string)GetValue(XmlFilenameProperty); }
            set { SetValue(XmlFilenameProperty, value); }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
