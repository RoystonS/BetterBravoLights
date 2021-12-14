using System;
using HidSharp;
using NLog;

namespace BravoLights.Common
{
    public interface IUsbLogic
    {
        bool LightsEnabled { get; set; }
    }

    public class UsbLogic : IUsbLogic
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
                return;
            }

            logger.Debug("Found Honeycomb Bravo: {0}", device.DevicePath);

            bravoDevice = device;
            bravoStream = device.Open();
        }

        private void DeviceList_Changed(object sender, DeviceListChangedEventArgs e)
        {
            this.CheckBravo();
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

                lightsEnabled = value;
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
