using System;
using System.Collections.Generic;
using BravoLights.Common;
using BravoLights.Connections;
using BravoLights.UI;

namespace BravoLights
{
    public class MainViewModel : ViewModelBase
    {
        private readonly ISet<string> litLights = new HashSet<string>();

        private Dictionary<string, LightExpression> lightExpressions = new();

        public IReadOnlyDictionary<string, LightExpression> LightExpressions
        {
            get { return lightExpressions; }
        }

        public void RegisterLights(IEnumerable<LightExpression> lights)
        {
            var newExpressions = new Dictionary<string, LightExpression>();

            foreach (var light in lights)
            {
                var lightName = light.LightName;

                newExpressions[lightName] = light;
                light.ValueChanged += ExpressionValueChanged;
            }

            foreach (var light in lightExpressions)
            {
                var expression = light.Value;
                if (expression != null)
                {
                    expression.ValueChanged -= ExpressionValueChanged;
                }
            }

            SetProperty(ref lightExpressions, newExpressions, nameof(LightExpressions));
        }

        private void ExpressionValueChanged(object sender, ValueChangedEventArgs e)
        {
            var lightExpression = (LightExpression)sender;
            var lightName = lightExpression.LightName;


            var lit = e.NewValue is not Exception && (bool)e.NewValue;

            bool changed;
            if (lit)
            {
                changed = litLights.Add(lightName);
            } else
            {
                changed = litLights.Remove(lightName);
            }

            if (changed)
            {
                RaisePropertyChanged(lightName);
            }
        }

        private string aircraft = "General";
        public string Aircraft
        {
            get { return aircraft; }
            set
            {
                SetProperty(ref aircraft, value);
            }
        }

        private SimState simState;
        public SimState SimState
        {
            get { return simState; }
            set { SetProperty(ref simState, value); }
        }

        public IEnumerable<string> LitLights
        {
            get { return litLights; }
        }

        public bool IsLit(string lightName)
        {
            return litLights.Contains(lightName);
        }
        public bool HDG { get { return IsLit(LightNames.HDG); } }
        public bool NAV { get { return IsLit(LightNames.NAV); } }
        public bool APR { get { return IsLit(LightNames.APR); } }
        public bool REV { get { return IsLit(LightNames.REV); } }
        public bool ALT { get { return IsLit(LightNames.ALT); } }
        public bool VS { get { return IsLit(LightNames.VS); } }
        public bool IAS { get { return IsLit(LightNames.IAS); } }
        public bool AUTOPILOT { get { return IsLit(LightNames.AUTOPILOT); } }

        public bool MasterWarning { get { return IsLit(LightNames.MasterWarning); } }
        public bool EngineFire { get { return IsLit(LightNames.EngineFire); } }
        public bool LowOilPressure { get { return IsLit(LightNames.LowOilPressure); } }
        public bool LowFuelPressure { get { return IsLit(LightNames.LowFuelPressure); } }
        public bool AntiIce { get { return IsLit(LightNames.AntiIce); } }
        public bool StarterEngaged { get { return IsLit(LightNames.StarterEngaged); } }
        public bool APU { get { return IsLit(LightNames.APU); } }
        public bool MasterCaution { get { return IsLit(LightNames.MasterCaution); } }
        public bool Vacuum { get { return IsLit(LightNames.Vacuum); } }
        public bool LowHydPressure { get { return IsLit(LightNames.LowHydPressure); } }
        public bool AuxFuelPump { get { return IsLit(LightNames.AuxFuelPump); } }
        public bool ParkingBrake { get { return IsLit(LightNames.ParkingBrake); } }
        public bool LowVolts { get { return IsLit(LightNames.LowVolts); } }
        public bool Door { get { return IsLit(LightNames.Door); } }

        public bool GearCRed {  get { return IsLit(LightNames.GearCRed); } }
        public bool GearCGreen { get { return IsLit(LightNames.GearCGreen); } }
        public bool GearLRed { get { return IsLit(LightNames.GearLRed); } }
        public bool GearLGreen { get { return IsLit(LightNames.GearLGreen); } }
        public bool GearRRed { get { return IsLit(LightNames.GearRRed); } }
        public bool GearRGreen { get { return IsLit(LightNames.GearRGreen); } }
    }
}
