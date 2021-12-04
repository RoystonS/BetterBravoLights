using System;
using System.Diagnostics;
using System.Timers;
using System.Windows;

namespace BravoLights.UI
{
    /// <summary>
    /// Interaction logic for BBLSplashScreen.xaml
    /// </summary>
    public partial class BBLSplashScreen : Window
    {
        private const int NewCheckTimeoutMillis = 5000;
        private const int MinimumSplashShowMillis = 1500;
        private const int MinimumNewVersionShowMillis = 20000;

        private DateTime showStart;

        public BBLSplashScreen()
        {
            InitializeComponent();

            showStart = DateTime.UtcNow;

            newVersionCheckTimeout = new Timer { Interval = NewCheckTimeoutMillis, AutoReset = false };
            newVersionCheckTimeout.Elapsed += NewVersionCheckTimer_Elapsed;
            CheckForNewVersion();
        }

        public static string ProductAndVersion
        {
            get { return ProgramInfo.ProductNameAndVersion; }
        }

        private Timer newVersionCheckTimeout;

        private async void CheckForNewVersion()
        {
            newVersionCheckTimeout.Start();
            var latestVersion = await ProgramInfo.GetLatestVersionStringAsync();
            if (this.Visibility != Visibility.Visible)
            {
                // Already hidden.
                return;
            }
            newVersionCheckTimeout.Stop();

            if (latestVersion == ProgramInfo.VersionString)
            {
                // We have the latest version. Let's make sure the splash screen shows for our minimum time
                var remainingMinimumTime = MinimumSplashShowMillis - DateTime.UtcNow.Subtract(showStart).TotalMilliseconds;
                if (remainingMinimumTime > 0)
                {
                    var timer = new Timer { Interval = remainingMinimumTime, AutoReset = false };
                    timer.Elapsed += delegate { HideOnCorrectThread(); };
                    timer.Start();
                } else
                {
                    // The splash screen has already been up for long enough
                    HideOnCorrectThread();
                }
            }
            else
            {
                // This is not the latest version; let the user know
                Dispatcher.Invoke(delegate
                {
                    NewVersionLinkText.Text = $"A new version ({latestVersion}) is available";
                });

                // If there's a new version, we want to ensure that's shown for a bit
                var timer = new Timer { Interval = MinimumNewVersionShowMillis, AutoReset = false };
                timer.Elapsed += delegate { HideOnCorrectThread(); };
                timer.Start();
            }
        }

        private void NewVersionCheckTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // We waited for 5 seconds to hear back from the version check, but it didn't work.
            // That's long enough to have the splash screen up, so just give up now.
            HideOnCorrectThread();
        }

        private void HideOnCorrectThread()
        {
            Dispatcher.Invoke(delegate { Hide(); });
        }

        private void NewVersionLink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            e.Handled = true;
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        }
    }
}
