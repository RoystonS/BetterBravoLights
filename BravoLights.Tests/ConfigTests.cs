using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Extensions;

using BravoLights;

namespace BravoLights.Tests
{
    public class ConfigTests
    {
        private static Config CreateConfig(string file)
        {
            var config = new Config();
            config.LoadConfig(file.Split('\r', '\n'));
            return config;
        }

        [Fact]
        public void IgnoresComments()
        {
            var config = CreateConfig(@"
;This is a comment
[Aircraft.Some_Aircraft]
;This is also a comment
REV = A:AUTOPILOT BACKCOURSE HOLD, bool == 1
;Another comment
");
            Assert.Equal("A:AUTOPILOT BACKCOURSE HOLD, bool == 1", config.GetConfig("Some_Aircraft", "REV"));
        }

        [Fact]
        public void ReturnsSpecificConfigurationsForMatchingAircraft()
        {
            var config = CreateConfig(@"
[Aircraft.Aircraft1]
REV = A:AUTOPILOT BACKCOURSE HOLD, bool == 1

[Aircraft.Aircraft2]
REV = A:AUTOPILOT BACKCOURSE HOLD, bool == 2
");
            Assert.Equal("A:AUTOPILOT BACKCOURSE HOLD, bool == 1", config.GetConfig("Aircraft1", "REV"));
            Assert.Equal("A:AUTOPILOT BACKCOURSE HOLD, bool == 2", config.GetConfig("Aircraft2", "REV"));
        }

        [Fact]
        public void MergesMultipleSectionsForASingleAircraft()
        {
            var config = CreateConfig(@"
[Aircraft.Aircraft1]
LowFuelPressure = 1 < 2
[Aircraft.Aircraft2]
LowFuelPressure = 2 < 3
[Aircraft.Aircraft1]
HDG = 3 > 4
");
            Assert.Equal("1 < 2", config.GetConfig("Aircraft1", "LowFuelPressure"));
            Assert.Equal("2 < 3", config.GetConfig("Aircraft2", "LowFuelPressure"));
            Assert.Equal("3 > 4", config.GetConfig("Aircraft1", "HDG"));

        }
        [Fact]
        public void AllowsAircraftToOverrideOrFallbackToDefault()
        {
            var config = CreateConfig(@"
[Default]
LowVolts = A:ELECTRICAL MAIN BUS VOLTAGE:1, volts <= 28
Vacuum = A:PARTIAL PANEL VACUUM, Enum == 1

[Aircraft.Aircraft1]
LowVolts = A:ELECTRICAL MAIN BUS VOLTAGE:3, volts <= 26
");
            Assert.Equal("A:ELECTRICAL MAIN BUS VOLTAGE:3, volts <= 26", config.GetConfig("Aircraft1", "LowVolts"));
            Assert.Equal("A:ELECTRICAL MAIN BUS VOLTAGE:1, volts <= 28", config.GetConfig("Aircraft2", "LowVolts"));
            Assert.Equal("A:PARTIAL PANEL VACUUM, Enum == 1", config.GetConfig("Aircraft1", "Vacuum"));
        }

        [Fact]
        public void AllowsCommaSeparatedAircraftAndSpecific()
        {
            var config = CreateConfig(@"
[Default]
LowVolts = A:ELECTRICAL MAIN BUS VOLTAGE:1, volts <= 28
Vacuum = A:PARTIAL PANEL VACUUM, Enum == 1
LowFuelPressure = OFF

[Aircraft.Aircraft1, Aircraft.Aircraft2]
LowVolts = A:ELECTRICAL MAIN BUS VOLTAGE:3, volts <= 26

[Aircraft.Aircraft1]
LowFuelPressure = A:GENERAL ENG FUEL PRESSURE:1, psf < 65
");
            Assert.Equal("A:ELECTRICAL MAIN BUS VOLTAGE:3, volts <= 26", config.GetConfig("Aircraft1", "LowVolts"));
            Assert.Equal("A:ELECTRICAL MAIN BUS VOLTAGE:3, volts <= 26", config.GetConfig("Aircraft2", "LowVolts"));
            Assert.Equal("A:GENERAL ENG FUEL PRESSURE:1, psf < 65", config.GetConfig("Aircraft1", "LowFuelPressure"));
            Assert.Equal("OFF", config.GetConfig("Aircraft2", "LowFuelPressure"));
        }

        [Fact]
        public void ReturnsNullForMissingConfiguration()
        {
            var config = CreateConfig(@"
[Default]
LowFuelPressure = OFF
");
            Assert.Equal("OFF", config.GetConfig("Aircraft1", "LowFuelPressure"));
            Assert.Null(config.GetConfig("Aircraft1", "EngineFire"));
        }
    }
}
