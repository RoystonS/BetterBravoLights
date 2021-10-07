using System;
using System.Timers;
using System.Windows;

namespace BravoLights.UI
{
    /// <summary>
    /// Interaction logic for BBLSplashScreen.xaml
    /// </summary>
    public partial class BBLSplashScreen : Window
    {
        public BBLSplashScreen()
        {
            InitializeComponent();
        }

        public static string ProductAndVersion
        {
            get { return ProgramInfo.ProductNameAndVersion; }
        }

        public void HideAfter(TimeSpan interval)
        {
            var timer = new Timer { Interval = interval.TotalMilliseconds };

            timer.Elapsed += delegate
            {
                Dispatcher.Invoke(delegate { Hide(); });
            };
            timer.Start();
        }
    }
}
