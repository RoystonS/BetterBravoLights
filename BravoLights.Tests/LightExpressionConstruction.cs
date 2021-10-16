using System.Collections.Generic;
using System.Linq;
using BravoLights.Common;
using Xunit;

namespace BravoLights.Tests
{
    public class LightExpressionConstruction
    {

        [Fact]
        public void ConstructsLightExpressionsFromBasicConfig()
        {
            var config = new Config();
            config.LoadConfig(new string[]
            {
                "[Default]",
                "HDG = ON",
                "NAV = OFF",
                "ALT = A:SOME VAR, bool == 1",
            });

            var lightExpressions = LightExpressionConfig.ComputeLightExpressions(config, "General");

            Assert.Equal("ON", FindLight(lightExpressions, "HDG").Expression.ToString());
            Assert.Equal("OFF", FindLight(lightExpressions, "NAV").Expression.ToString());
            Assert.Equal("(A:SOME VAR, bool == 1)", FindLight(lightExpressions, "ALT").Expression.ToString());
        }

        [Fact]
        public void CanInvertSelectedLights()
        {
            var config = new Config();
            config.LoadConfig(new string[]
            {
                "[Default]",
                "HDG = ON",
                "NAV = OFF",
                "ALT = ON",
                "Invert = HDG, NAV"
            });

            var lightExpressions = LightExpressionConfig.ComputeLightExpressions(config, "General");

            Assert.Equal("(NOT ON)", FindLight(lightExpressions, "HDG").Expression.ToString());
            Assert.Equal("(NOT OFF)", FindLight(lightExpressions, "NAV").Expression.ToString());
            Assert.Equal("ON", FindLight(lightExpressions, "ALT").Expression.ToString());
            
            // And there should be no light configuration for a light called 'Invert'
            Assert.Null(FindLight(lightExpressions, "Invert"));
        }

        [Fact]
        public void SupportsAircraftSpecificInversion()
        {
            var config = new Config();
            config.LoadConfig(new string[]
            {
                "[Default]",
                "HDG = ON",
                "NAV = OFF",
                "ALT = ON",
                "[Aircraft.AC1]",
                "Invert = NAV"
            });

            var lightExpressions = LightExpressionConfig.ComputeLightExpressions(config, "AC1");

            Assert.Equal("ON", FindLight(lightExpressions, "HDG").Expression.ToString());
            Assert.Equal("(NOT OFF)", FindLight(lightExpressions, "NAV").Expression.ToString());
            Assert.Equal("ON", FindLight(lightExpressions, "ALT").Expression.ToString());
        }

        private static LightExpression FindLight(IEnumerable<LightExpression> lightExpressions, string lightName)
        {
            return lightExpressions.Where(le => le.LightName == lightName).SingleOrDefault();
        }
    }
}
