using System;
using System.ComponentModel;
using HidSharp;
using NLog;

namespace BravoLights.Common
{
    public interface IUsbLogic : INotifyPropertyChanged
    {
        /// <summary>
        /// Gets or sets a value indicating whether we should be showing lights on the Bravo.
        /// If false, all lights will be switched off. If true, lights will be shown based on
        /// the contents of the <see cref="ILightsState"/>.
        /// </summary>
        bool LightsEnabled { get; set; }

        /// <summary>
        /// Gets a value indicating whether a Bravo Throttle is actually present.
        /// </summary>
        bool BravoPresent { get; }
    }

    public class UsbLogic : ViewModelBase, IUsbLogic
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private static readonly int HoneycombVendorId = 0x294B;
        private static readonly int BravoProductId = 0x1901;

        private readonly ILightsState lightsState;

        private HidDevice bravoDevice;
        private HidStream bravoStream;

        public UsbLogic(ILightsState lightsState)
        {
            this.lightsState = lightsState;

            this.CheckBravo();

            this.LightsState_Changed(null, EventArgs.Empty);

            DeviceList.Local.Changed += DeviceList_Changed;

            lightsState.PropertyChanged += LightsState_Changed;
        }

        private void CheckBravo()
        {
            var device = DeviceList.Local.GetHidDeviceOrNull(vendorID: HoneycombVendorId, productID: BravoProductId);
            if (device != bravoDevice)
            {
                // There's either no Bravo or a different one. Drop the old connection.
                if (bravoStream != null)
                {
                    logger.Info("Disconnecting from Bravo");
                    bravoStream.Close();
                    bravoStream = null;
                    bravoDevice = null;
                }
            }

            if (device == null)
            {
                logger.Warn("No Honeycomb Bravo device found");
                this.BravoPresent = false;
                return;
            }

            logger.Debug("Found Honeycomb Bravo: {0}", device.DevicePath);

            bravoDevice = device;
            bravoStream = device.Open();

            LightsState_Changed(null, EventArgs.Empty);
            this.BravoPresent = true;
        }

        private void DeviceList_Changed(object sender, DeviceListChangedEventArgs e)
        {
            this.CheckBravo();
        }
        
        private bool bravoPresent = false;
        public bool BravoPresent
        {
            get { return bravoPresent; }
            private set
            {
                SetProperty(ref bravoPresent, value);
            }
        }

        private bool lightsEnabled = false;

        public bool LightsEnabled
        {
            get { return lightsEnabled; }
            set {
                if (lightsEnabled == value)
                {
                    return;
                }

                logger.Debug("LightsEnabled = {0}", value);

                SetProperty(ref lightsEnabled, value);
                LightsState_Changed(null, EventArgs.Empty);
            }
        }

        private void LightsState_Changed(object sender, EventArgs e)
        {
            var data = new byte[] { 0, 0, 0, 0, 0 };

            if (LightsEnabled)
            {
                foreach (var light in lightsState.LitLights)
                {
                    var lightInfo = LightNames.LightInfos[light];
                    data[lightInfo.Byte] |= lightInfo.BitValue;
                }
            }

            if (bravoStream != null)
            {
                bravoStream.SetFeature(data);
            }
        }
    }
}
