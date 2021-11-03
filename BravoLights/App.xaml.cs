using System;
using System.Linq;
using System.Windows;
using System.Windows.Interop;
using Forms = System.Windows.Forms;

using BravoLights.Connections;
using BravoLights.Installation;
using BravoLights.UI;
using System.Drawing;
using BravoLights.Ast;
using BravoLights.Common;
using System.Collections.Generic;
using System.Diagnostics;

namespace BravoLights
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private BBLSplashScreen splashScreen;
        private LightsWindow lightsWindow;

        private MainViewModel viewModel;
        private UsbLogic usbLogic;
        private GlobalLightController globalLightController;
        private Config config;

        private Forms.NotifyIcon notifyIcon;

        private bool exitWhenSimulatorExits = false;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (e.Args.Length == 1)
            {
                var cmd = e.Args[0];
                
                try
                {
                    switch (cmd)
                    {
                        case "/install":
                            {
                                var msg = Installer.Install();
                                MessageBox.Show(msg, "Better Bravo Lights");
                                Environment.Exit(0);
                                break;
                            }
                        case "/uninstall":
                            {
                                var msg = Installer.Uninstall();
                                MessageBox.Show(msg, "Better Bravo Lights");
                                Environment.Exit(0);
                                break;
                            }
                        case "/startedbysimulator":
                            exitWhenSimulatorExits = true;
                            break;
                    }
                }
                catch (CorruptExeXmlException ex)
                {
                    var window = new CorruptExeXmlErrorWindow() { XmlFilename = ex.ExeXmlFilename, Exception = ex };
                    window.ShowDialog();
                    Environment.Exit(0);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Operation failed: {ex.GetType().FullName}:{ex.Message}. Please report this to the application author.", "Better Bravo Lights", MessageBoxButton.OK, MessageBoxImage.Error);
                    Environment.Exit(0);
                }
            }
            else
            {
                // Run without command arguments; detect whether it should automatically exit
                if (Installer.IsSetToRunOnStartup && Installer.InstallationNeedsUpdating)
                {
                    // We are set to run on simulator startup but the installation entries are out of date
                    try
                    {
                        Installer.Install();
                    } catch { }
                    
                    // And for this run, assume we were run with the simulator
                    exitWhenSimulatorExits = true;
                }
            }

            var processName = Process.GetCurrentProcess().ProcessName;
            if (Process.GetProcessesByName(processName).Length > 1)
            {
                // There was already a copy running.
                if (exitWhenSimulatorExits)
                {
                    // It's being started by the simulator. Exit silently.
                    Environment.Exit(0);
                } else
                {
                    MessageBox.Show($"Another copy of Better Bravo Lights is already running.", "Better Bravo Lights", MessageBoxButton.OK);
                    Environment.Exit(0);
                }
            }

            var includedWasmVersion = Installer.IncludedWasmModuleVersion;
            var installedWasmVersion = Installer.InstalledWasmModuleVersion;

            if (installedWasmVersion != includedWasmVersion)
            {
                var doInstall = false;

                if (installedWasmVersion == null)
                {
                    if (MessageBox.Show("Lights that use L: variables will not work correctly unless the Better Bravo Lights WASM module is installed in the Flight Simulator Community folder. Would you like it to be installed?",
                        "Better Bravo Lights", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        doInstall = true;
                    }
                }
                else
                {
                    if (MessageBox.Show("Lights that use L: variables will not work correctly unless the correct version of the Better Bravo Lights WASM module is installed in the Flight Simulator Community folder. Would you like the correct version to be installed?",
                        "Better Bravo Lights", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        doInstall = true;
                    }
                }
                if (doInstall)
                {
                    Installer.InstallWasmModule();
                    MessageBox.Show("The Better Bravo Lights WASM module is now installed. If Flight Simulator was already running it will need to be restarted.", "Better Bravo Lights", MessageBoxButton.OK);
                } else
                {
                    LVarManager.Connection.DisableLVars = true;
                }
            }

            viewModel = new MainViewModel();
            usbLogic = new UsbLogic(viewModel);
            globalLightController = new GlobalLightController(usbLogic);

            splashScreen = new BBLSplashScreen();
            splashScreen.Show();
            splashScreen.HideAfter(TimeSpan.FromSeconds(1.5));

            lightsWindow = new LightsWindow
            {
                ViewModel = viewModel
            };
            
            // How can we get an HWnd without having to (briefly) show the lights window?
            var hwndWindow = splashScreen;
            var hwndSource = PresentationSource.FromVisual(hwndWindow) as HwndSource;
            hwndSource.AddHook(WndProc);

            SimConnectConnection.HWnd = new WindowInteropHelper(hwndWindow).Handle;
            SimConnectConnection.Connection.OnAircraftLoaded += Connection_OnAircraftLoaded;
            SimConnectConnection.Connection.OnSimStateChanged += Connection_OnSimStateChanged;
            SimConnectConnection.Connection.OnInMainMenuChanged += Connection_OnInMainMenuChanged;
            Connection_OnInMainMenuChanged(null, EventArgs.Empty);

            var toolStrip = new Forms.ContextMenuStrip();
            toolStrip.Items.Add(new Forms.ToolStripLabel { Text = ProgramInfo.ProductNameAndVersion });

            var btnDebug = new Forms.ToolStripButton(BravoLights.Properties.Resources.TrayIconMenuDebugger)
            {
                Dock = Forms.DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Image = BravoLights.Properties.Resources.DebuggerImage,
                ImageAlign = ContentAlignment.MiddleLeft
            };
            btnDebug.Click += BtnDebug_Click;          
            toolStrip.Items.Add(btnDebug);

            var btnExit = new Forms.ToolStripButton(BravoLights.Properties.Resources.TrayIconMenuExit)
            {
                Dock = Forms.DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Image = BravoLights.Properties.Resources.ExitImage,
                ImageAlign = ContentAlignment.MiddleLeft
            };
            btnExit.Click += BtnExit_Click;
            toolStrip.Items.Add(btnExit);

            notifyIcon = new Forms.NotifyIcon
            {
                Icon = BravoLights.Properties.Resources.TrayIcon,
                Visible = true,
                ContextMenuStrip = toolStrip,
            };
            notifyIcon.DoubleClick += BtnDebug_Click;
            UpdateTrayIconText();

            config = new Config();
            config.OnConfigChanged += Config_OnConfigChanged;
            config.Monitor();

            // Strictly speaking we only really need to connect once we have variable-based light expressions registered,
            // but in practice we want to know if the sim has exited, even if we never use it.
            SimConnectConnection.Connection.Start();
        }

        private void Connection_OnInMainMenuChanged(object sender, EventArgs e)
        {
            globalLightController.SimulatorInMainMenu = SimConnectConnection.Connection.InMainMenu;
        }

        private void BtnExit_Click(object sender, EventArgs e)
        {
            ExitApplication();
        }

        private void BtnDebug_Click(object sender, EventArgs e)
        {
            lightsWindow.Show();
            lightsWindow.Activate();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            notifyIcon.Dispose();
            base.OnExit(e);
        }

        private const int WM_USER_SIMCONNECT = 0x0402;

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_USER_SIMCONNECT)
            {
                handled = true;
                SimConnectConnection.Connection.ReceiveMessage();
            }

            return IntPtr.Zero;
        }

        private void Connection_OnSimStateChanged(object sender, SimStateEventArgs e)
        {
            viewModel.SimState = e.SimState;

            globalLightController.SimulatorConnected = (e.SimState == SimState.SimRunning);

            switch (e.SimState)
            {
                case SimState.SimExited:
                    if (exitWhenSimulatorExits)
                    {
                        ExitApplication();
                    } else
                    {
                        // Attempt to reconnect when the sim starts again
                        SimConnectConnection.Connection.Start();
                    }

                    break;
            }

            UpdateTrayIconText();
        }

        private void UpdateTrayIconText()
        {
            var format = viewModel.SimState == SimState.SimRunning ? BravoLights.Properties.Resources.TrayIconConnectedToSimFormat : BravoLights.Properties.Resources.TrayIconWaitingForSimFormat;

            notifyIcon.Text = String.Format(format, ProgramInfo.ProductNameAndVersion);
        }

        private void Connection_OnAircraftLoaded(object sender, AircraftEventArgs e)
        {
            if (e.Aircraft != viewModel.Aircraft)
            {
                viewModel.Aircraft = e.Aircraft;
                Config_OnConfigChanged(null, null);
            }
        }

        private void Config_OnConfigChanged(object sender, EventArgs e)
        {
            // Turn off the lights whilst we reconfigure everything
            globalLightController.ReadingConfiguration = true;

            var lightExpressions = LightExpressionConfig.ComputeLightExpressions(config, viewModel.Aircraft);
            viewModel.RegisterLights(lightExpressions);

            globalLightController.ReadingConfiguration = false;
        }

        private void ExitApplication()
        {
            globalLightController.ApplicationExiting = true;
            notifyIcon.Dispose();
            Current.Shutdown();
        }
    }
}
