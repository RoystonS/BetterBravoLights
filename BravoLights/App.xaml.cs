using System;
using System.Windows;
using System.Windows.Interop;
using Forms = System.Windows.Forms;
using BravoLights.Connections;
using BravoLights.Installation;
using BravoLights.UI;
using System.Drawing;
using BravoLights.Common;
using System.Diagnostics;
using NLog;
using System.IO;

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
        private IConfig config;

        private Forms.NotifyIcon notifyIcon;

        private bool startedBySimulator = false;
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private VariableList variableList;

        private void TryInstallOrUninstall(string cmd)
        {
            try
            {
                switch (cmd)
                {
                    case "/install":
                        {
                            var msg = Installer.Install();
                            MessageBox.Show(msg, "Better Bravo Lights");
                            logger.Debug("Install:Exit");
                            Environment.Exit(0);
                            break;
                        }
                    case "/uninstall":
                        {
                            var msg = Installer.Uninstall();
                            MessageBox.Show(msg, "Better Bravo Lights");
                            logger.Debug("Uninstall:Exit");
                            Environment.Exit(0);
                            break;
                        }
                }
            }
            catch (CorruptExeXmlException ex)
            {
                // The exe.xml file is broken. Maybe we can fix it?
                if (ex.RepairedContent != null)
                {
                    // Yes! Fix the exe.xml file
                    File.WriteAllText(ex.ExeXmlFilename, ex.RepairedContent);
                    // Go try the install/uninstall again
                    TryInstallOrUninstall(cmd);
                    return;
                }
                else
                {
                    var window = new CorruptExeXmlErrorWindow() { Exception = ex };
                    window.ShowDialog();
                    Environment.Exit(0);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Operation failed: {ex.GetType().FullName}:{ex.Message}. Please report this to the application author.", "Better Bravo Lights", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(0);
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            logger.Debug("Starting");

            logger.Debug("Arguments {0}", string.Join(" ", e.Args));

            if (e.Args.Length == 1)
            {
                var cmd = e.Args[0];

                if (cmd == "/startedbysimulator")
                {
                    startedBySimulator = true;
                    logger.Debug("Started by simulator");
                }

                TryInstallOrUninstall(cmd);
            }
            else
            {
                logger.Debug("Run without arguments");

                // Run without command arguments; detect whether it should automatically exit
                if (Installer.IsSetToRunOnStartup && Installer.InstallationNeedsUpdating)
                {
                    logger.Debug("Need to update installation");

                    // We are set to run on simulator startup but the installation entries are out of date
                    try
                    {
                        Installer.Install();
                    }
                    catch { }
                }
            }

            var processName = Process.GetCurrentProcess().ProcessName;
            if (Process.GetProcessesByName(processName).Length > 1)
            {
                logger.Debug("Existing copy already running");

                // There was already a copy running.
                if (startedBySimulator)
                {
                    // It's being started by the simulator. Exit silently.
                    Environment.Exit(0);
                }
                else
                {
                    MessageBox.Show($"Another copy of Better Bravo Lights is already running.", "Better Bravo Lights", MessageBoxButton.OK);
                    Environment.Exit(0);
                }
            }

            var includedWasmVersion = Installer.IncludedWasmModuleVersion;
            var installedWasmVersion = Installer.InstalledWasmModuleVersion;

            logger.Debug("Installed WASM {0}, included WASM {1}", installedWasmVersion, includedWasmVersion);

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
                }
                else
                {
                    LVarManager.Connection.DisableLVars = true;
                }
            }

            viewModel = new MainViewModel();
            usbLogic = new UsbLogic(viewModel);
            globalLightController = new GlobalLightController(usbLogic);

            splashScreen = new BBLSplashScreen();
            splashScreen.Show();

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
            var productLabel = new Forms.ToolStripLabel { Text = ProgramInfo.ProductNameAndVersion };
            toolStrip.Items.Add(productLabel);

            CheckForNewVersionAsync(productLabel);

            var btnDebug = new Forms.ToolStripButton(BravoLights.Properties.Resources.TrayIconMenuDebugger)
            {
                Dock = Forms.DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Image = BravoLights.Properties.Resources.DebuggerImage,
                ImageAlign = ContentAlignment.MiddleLeft
            };
            btnDebug.Click += BtnDebug_Click;
            toolStrip.Items.Add(btnDebug);

            var btnVariableList = new Forms.ToolStripButton("Variable List (Experimental)")
            {
                Dock = Forms.DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Image = BravoLights.Properties.Resources.TableImage,
                ImageAlign = ContentAlignment.MiddleLeft
            };
            btnVariableList.Click += BtnVariableList_Click;
            toolStrip.Items.Add(btnVariableList);

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

            var userConfig = new FileConfig(FlightSimulatorPaths.UserConfigIniPath);
            var builtInConfig = new FileConfig(FlightSimulatorPaths.BuiltInConfigIniPath);
            config = new ConfigChain(userConfig, builtInConfig);
            config.OnConfigChanged += Config_OnConfigChanged;
            userConfig.Monitor();
            builtInConfig.Monitor();

            // Strictly speaking we only really need to connect once we have variable-based light expressions registered,
            // but in practice we want to know if the sim has exited, even if we never use it.
            SimConnectConnection.Connection.Start();
        }

        private async void CheckForNewVersionAsync(Forms.ToolStripLabel productLabel)
        {
            if (await ProgramInfo.IsNewVersionAvailableAsync())
            {
                productLabel.Text += $" (New version {await ProgramInfo.GetLatestVersionStringAsync()} available)";
            }
        }

        private void Connection_OnInMainMenuChanged(object sender, EventArgs e)
        {
            globalLightController.SimulatorInMainMenu = SimConnectConnection.Connection.InMainMenu;
        }

        private void BtnExit_Click(object sender, EventArgs e)
        {
            logger.Debug("User requested exit");
            ExitApplication();
        }

        private void BtnDebug_Click(object sender, EventArgs e)
        {
            logger.Debug("User requested debugger UI");

            lightsWindow.Show();
            lightsWindow.Activate();
        }

        private void BtnVariableList_Click(object sender, EventArgs e)
        {
            if (variableList == null)
            {
                variableList = new VariableList();
                variableList.Show();

                variableList.Closed += delegate
                {
                    variableList = null;
                };
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            logger.Debug("OnExit");

            if (notifyIcon != null)
            {
                notifyIcon.Dispose();
            }
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

            logger.Debug("SimState {0}", e.SimState);

            switch (e.SimState)
            {
                case SimState.SimExited:
                    // MSFS has exited, so we will too.
                    ExitApplication();
                    return;
            }

            UpdateTrayIconText();
        }

        private void UpdateTrayIconText()
        {
            var format = viewModel.SimState == SimState.SimRunning ? BravoLights.Properties.Resources.TrayIconConnectedToSimFormat : BravoLights.Properties.Resources.TrayIconWaitingForSimFormat;

            notifyIcon.Text = string.Format(format, ProgramInfo.ProductNameAndVersion);
        }

        private void Connection_OnAircraftLoaded(object sender, AircraftEventArgs e)
        {
            if (e.Aircraft != viewModel.Aircraft)
            {
                logger.Debug("Aircraft loaded: {0}", e.Aircraft);

                viewModel.Aircraft = e.Aircraft;
                Config_OnConfigChanged(null, null);
            }
        }

        private void Config_OnConfigChanged(object sender, EventArgs e)
        {
            logger.Debug("ConfigChanged");

            // Turn off the lights whilst we reconfigure everything
            globalLightController.ReadingConfiguration = true;

            var lightExpressions = LightExpressionConfig.ComputeLightExpressions(config, viewModel.Aircraft);
            viewModel.RegisterLights(lightExpressions);

            globalLightController.ReadingConfiguration = false;
        }

        private void ExitApplication()
        {
            logger.Debug("Application exiting");
            globalLightController.ApplicationExiting = true;
            notifyIcon.Dispose();
            Current.Shutdown();
        }
    }
}
