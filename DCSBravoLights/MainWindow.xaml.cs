using System;
using System.Windows;
using BravoLights.Common;

namespace DCSBravoLights
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private readonly DcsBiosState dcsBiosState = new();
        private DebuggerUI debuggerUI;
        private LightsState lightsState;
#pragma warning disable IDE0052 // Remove unread private members
        private UsbLogic usbLogic;
#pragma warning restore IDE0052 // Remove unread private members

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            dcsBiosState.StartListening();

            var variablesManager = new DcsVariablesManager(dcsBiosState);

            DcsConnection.Connection.DcsVariablesManager = variablesManager;

            debuggerUI = new();
            debuggerUI.Show();

            lightsState = new LightsState();
            usbLogic = new UsbLogic(lightsState)
            {
                LightsEnabled = true
            };

            Monitor(LightNames.GearLGreen, "[Landing Gear and Flap Control Panel:GEAR_L_SAFE] == 1");
            Monitor(LightNames.GearCGreen, "[Landing Gear and Flap Control Panel:GEAR_N_SAFE] == 1");
            Monitor(LightNames.GearRGreen, "[Landing Gear and Flap Control Panel:GEAR_R_SAFE] == 1");
            Monitor(LightNames.GearLRed, "[Landing Gear and Flap Control Panel:GEAR_L_SAFE] == 0 AND [Landing Gear and Flap Control Panel:HANDLE_GEAR_WARNING] == 1");
            Monitor(LightNames.GearCRed, "[Landing Gear and Flap Control Panel:GEAR_N_SAFE] == 0 AND [Landing Gear and Flap Control Panel:HANDLE_GEAR_WARNING] == 1");
            Monitor(LightNames.GearRRed, "[Landing Gear and Flap Control Panel:GEAR_R_SAFE] == 0 AND [Landing Gear and Flap Control Panel:HANDLE_GEAR_WARNING] == 1");
            Monitor(LightNames.MasterCaution, "[UFC:MASTER_CAUTION] == 1");
            Monitor(LightNames.LowOilPressure, "[Caution Lights Panel:CL_F2] == 1 OR [Caution Lights Panel:CL_F3] == 1");
            Monitor(LightNames.LowHydPressure, "[Caution Lights Panel:CL_A2] == 1 OR [Caution Lights Panel:CL_A3] == 1");
            Monitor(LightNames.LowFuelPressure, "[Caution Lights Panel:CL_J2] == 1 OR [Caution Lights Panel:CL_J3] == 1");
            Monitor(LightNames.AuxFuelPump, "[Caution Lights Panel:CL_G2] == 1 OR [Caution Lights Panel:CL_G3] == 1 OR [Caution Lights Panel:CL_H2] == 1 OR [Caution Lights Panel:CL_H3] == 1");
            Monitor(LightNames.EngineFire, "[Glare Shield:APU_FIRE] == 1 OR [Glare Shield:L_ENG_FIRE] == 1 OR [Glare Shield:R_ENG_FIRE] == 1");
            Monitor(LightNames.StarterEngaged, "[Caution Lights Panel:CL_A1] == 1");
            Monitor(LightNames.APU, "[Caution Lights Panel:CL_L1] == 1");
            Monitor(LightNames.Door, "[Misc:CANOPY_VALUE] > 0");
            Monitor(LightNames.AntiIce, "[Environment Control Panel:ENVCP_PITOT_HEAT] != 1");
            Monitor(LightNames.LowVolts, "[Caution Lights Panel:CL_L2] == 1 OR [Caution Lights Panel:CL_L3] == 1");
        }

        private void Monitor(string lightName, string expression)
        {
            var lightExpression = new LightExpression(lightName, DcsExpressionParser.Parse(expression), true);

            lightExpression.ValueChanged += LightExpression_ValueChanged;
        }

        private void LightExpression_ValueChanged(object sender, ValueChangedEventArgs e)
        {
            var lightExpression = (LightExpression)sender;

            var lit = e.NewValue is not Exception && (bool)e.NewValue;
            lightsState.SetLight(lightExpression.LightName, lit);
        }
    }
}