using System.Collections.Generic;

namespace BravoLights.Common
{
    class LightInfo
    {
        public int Byte;
        public byte BitValue;
    }

    public static class LightNames
    {
        public const string HDG = "HDG";
        public const string NAV = "NAV";
        public const string APR = "APR";
        public const string REV = "REV";
        public const string ALT = "ALT";
        public const string VS = "VS";
        public const string IAS = "IAS";
        public const string AUTOPILOT = "AUTOPILOT";

        public const string GearLGreen = "GearLGreen";
        public const string GearLRed = "GearLRed";
        public const string GearCGreen = "GearCGreen";
        public const string GearCRed = "GearCRed";
        public const string GearRGreen = "GearRGreen";
        public const string GearRRed = "GearRRed";

        public const string MasterWarning = "MasterWarning";
        public const string EngineFire = "EngineFire";
        public const string LowOilPressure = "LowOilPressure";
        public const string LowFuelPressure = "LowFuelPressure";
        public const string AntiIce = "AntiIce";
        public const string StarterEngaged = "StarterEngaged";
        public const string APU = "APU";

        public const string MasterCaution = "MasterCaution";
        public const string Vacuum = "Vacuum";
        public const string LowHydPressure = "LowHydPressure";
        public const string AuxFuelPump = "AuxFuelPump";
        public const string ParkingBrake = "ParkingBrake";
        public const string LowVolts = "LowVolts";
        public const string Door = "Door";

        public static IEnumerable<string> AllNames
        {
            get { return LightInfos.Keys; }
        }

        internal static IDictionary<string, LightInfo> LightInfos = new Dictionary<string, LightInfo> {
            { HDG, new LightInfo { Byte = 1, BitValue = 1 << 0 }  },
            { NAV, new LightInfo { Byte = 1, BitValue = 1 << 1 }  },
            { APR, new LightInfo { Byte = 1, BitValue = 1 << 2 }  },
            { REV, new LightInfo { Byte = 1, BitValue = 1 << 3 }  },
            { ALT, new LightInfo { Byte = 1, BitValue = 1 << 4 }  },
            { VS, new LightInfo { Byte = 1, BitValue = 1 << 5 }  },
            { IAS, new LightInfo { Byte = 1, BitValue = 1 << 6 }  },
            { AUTOPILOT, new LightInfo { Byte = 1, BitValue = 1 << 7 }  },

            { GearLGreen, new LightInfo { Byte = 2, BitValue = 1 << 0 } },
            { GearLRed, new LightInfo { Byte = 2, BitValue = 1 << 1 } },
            { GearCGreen, new LightInfo { Byte = 2, BitValue = 1 << 2 } },
            { GearCRed, new LightInfo { Byte = 2, BitValue = 1 << 3 } },
            { GearRGreen, new LightInfo { Byte = 2, BitValue = 1 << 4 } },
            { GearRRed, new LightInfo { Byte = 2, BitValue = 1 << 5 } },

            { MasterWarning, new LightInfo { Byte = 2, BitValue = 1 << 6 } },
            { EngineFire, new LightInfo { Byte = 2, BitValue = 1 << 7 } },
            { LowOilPressure, new LightInfo { Byte = 3, BitValue = 1 << 0 } },
            { LowFuelPressure, new LightInfo { Byte = 3, BitValue = 1 << 1 } },
            { AntiIce, new LightInfo { Byte = 3, BitValue = 1 << 2 } },
            { StarterEngaged, new LightInfo { Byte = 3, BitValue = 1 << 3 } },
            { APU, new LightInfo { Byte = 3, BitValue = 1 << 4 } },

            { MasterCaution, new LightInfo { Byte = 3, BitValue = 1 << 5 } },
            { Vacuum, new LightInfo { Byte = 3, BitValue = 1 << 6 } },
            { LowHydPressure, new LightInfo { Byte = 3, BitValue = 1 << 7 } },                     
            { AuxFuelPump, new LightInfo { Byte = 4, BitValue = 1 << 0 } },
            { ParkingBrake, new LightInfo { Byte = 4, BitValue = 1 << 1 } },
            { LowVolts, new LightInfo { Byte = 4, BitValue = 1 << 2 } },
            { Door, new LightInfo { Byte = 4, BitValue = 1 << 3 } },
        };
    }
}
