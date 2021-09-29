using System.Collections.Generic;

namespace BravoLights
{
    class LightInfo
    {
        public int Byte;
        public byte BitValue;
    }

    static class LightNames
    {
        public static string HDG = "HDG";
        public static string NAV = "NAV";
        public static string APR = "APR";
        public static string REV = "REV";
        public static string ALT = "ALT";
        public static string VS = "VS";
        public static string IAS = "IAS";
        public static string AUTOPILOT = "AUTOPILOT";

        public static string GearLGreen = "GearLGreen";
        public static string GearLRed = "GearLRed";
        public static string GearCGreen = "GearCGreen";
        public static string GearCRed = "GearCRed";
        public static string GearRGreen = "GearRGreen";
        public static string GearRRed = "GearRRed";

        public static string MasterWarning = "MasterWarning";
        public static string EngineFire = "EngineFire";
        public static string LowOilPressure = "LowOilPressure";
        public static string LowFuelPressure = "LowFuelPressure";
        public static string AntiIce = "AntiIce";
        public static string StarterEngaged = "StarterEngaged";
        public static string APU = "APU";

        public static string MasterCaution = "MasterCaution";
        public static string Vacuum = "Vacuum";
        public static string LowHydPressure = "LowHydPressure";
        public static string AuxFuelPump = "AuxFuelPump";
        public static string ParkingBrake = "ParkingBrake";
        public static string LowVolts = "LowVolts";
        public static string Door = "Door";

        public static IEnumerable<string> AllNames
        {
            get { return LightInfos.Keys; }
        }

        public static IDictionary<string, LightInfo> LightInfos = new Dictionary<string, LightInfo> {
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
