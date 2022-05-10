using System;
using System.Diagnostics;
using System.IO;
using System.Web;
using System.Windows;
using System.Windows.Navigation;
using BravoLights.Installation;

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

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            e.Handled = true;
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        }

        private void ShowExeXmlLocation(object sender, RequestNavigateEventArgs e)
        {
            e.Handled = true;
            ShowFolderLocation(Exception);
        }

        public static void ShowFolderLocation(CorruptExeXmlException exception)
        { 
            Process.Start(new ProcessStartInfo(Path.GetDirectoryName(exception.ExeXmlFilename)) { UseShellExecute = true });
        }

        private void RaiseExeXmlIssue(object sender, RequestNavigateEventArgs e)
        {
            e.Handled = true;
            RaiseGitHubIssue(Exception);
        }
        
        public static void RaiseGitHubIssue(CorruptExeXmlException exception)
        { 
            var queryParams = HttpUtility.ParseQueryString(string.Empty);
            queryParams["template"] = "50-help-fix-exe-xml.yaml";

            queryParams["labels"] = "exe-xml-help";
            queryParams["title"] = "Can you help fix my corrupt exe.xml file?";

            queryParams["bbl_error"] = exception.InnerException.Message;
            queryParams["bbl_version"] = ProgramInfo.VersionString;
            queryParams["bbl_exe_xml_path"] = exception.ExeXmlFilename;
            queryParams["bbl_exe_xml_contents"] = exception.OriginalContent;

            var uriBuilder = new UriBuilder("https://github.com/RoystonS/BetterBravoLights/issues/new")
            {
                Query = queryParams.ToString()
            };

            var uri = uriBuilder.ToString();

            Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
        }

        public static readonly DependencyProperty ExceptionProperty = DependencyProperty.Register("Exception", typeof(CorruptExeXmlException), typeof(CorruptExeXmlErrorWindow));
        public CorruptExeXmlException Exception
        {
            get { return (CorruptExeXmlException)GetValue(ExceptionProperty); }
            set { SetValue(ExceptionProperty, value); }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
