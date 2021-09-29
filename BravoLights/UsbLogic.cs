using System;
using System.Diagnostics;
using HidSharp;

namespace BravoLights
{
    class UsbLogic
    {
        private static int HoneycombVendorId = 0x294B;
        private static int BravoProductId = 0x1901;

        private MainViewModel lightsState;
        private HidDevice bravoDevice;
        private HidStream bravoStream;

        public UsbLogic(MainViewModel lightsState)
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
                    Console.WriteLine("Disconnecting from Bravo");
                    bravoStream.Close();
                    bravoStream = null;
                    bravoDevice = null;
                }
            }

            if (device == null)
            {
                Console.WriteLine("No Bravo found");
                return;
            }

            Console.WriteLine("Found Bravo");
            Console.WriteLine(device.DevicePath);

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
