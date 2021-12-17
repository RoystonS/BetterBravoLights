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
            var config = new FileConfig("Config.ini");
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
            var config = new FileConfig("Config.ini");
            config.LoadConfig(new string[]
            {
                "[Default]",
                "HDG = ON",
                "NAV = OFF",
                "ALT = ON",
                "Invert = HDG, NAV"
            });

            var lightExpressions = LightExpressionConfig.ComputeLightExpressions(config, "General");

            Assert.Equal("OFF", FindLight(lightExpressions, "HDG").Expression.ToString());
            Assert.Equal("ON", FindLight(lightExpressions, "NAV").Expression.ToString());
            Assert.Equal("ON", FindLight(lightExpressions, "ALT").Expression.ToString());
            
            // And there should be no light configuration for a light called 'Invert'
            Assert.Null(FindLight(lightExpressions, "Invert"));
        }

        [Fact]
        // This is the basic 'happy day' case for MasterEnable
        public void MasterEnableSettingIsAddedToExpressions()
        {
            var config = new FileConfig("Config.ini");
            config.LoadConfig(new string[] {
                "[Default]",
                "MasterEnable = A:VAR1, number == 1",
                "HDG = A:VARA, number == 2",
            });

            var lightExpressions = LightExpressionConfig.ComputeLightExpressions(config, "General");

            Assert.Equal("((A:VAR1, number == 1) AND (A:VARA, number == 2))", FindLight(lightExpressions, "HDG").Expression.ToString());
        }


        [Fact]
        public void AppliesBracketingToMasterEnableExpressions()
        {
            var config = new FileConfig("Config.ini");
            config.LoadConfig(new string[] {
                "[Default]",
                "MasterEnable = A:VAR1, number == 1 OR A:VAR2, number == 2",
                "HDG = A:VARA, number == 2 OR A:VARB, number == 3",
            });

            var lightExpressions = LightExpressionConfig.ComputeLightExpressions(config, "General");
            
            // This test makes sure that multiple parts of the MasterEnable expression and light expression don't combine
            // in incorrect ways.
            Assert.Equal("(((A:VAR1, number == 1) OR (A:VAR2, number == 2)) AND ((A:VARA, number == 2) OR (A:VARB, number == 3)))", FindLight(lightExpressions, "HDG").Expression.ToString());
        }

        [Fact]
        public void MasterEnableInheritsAcrossAircraft()
        {
            var config = new FileConfig("Config.ini");
            config.LoadConfig(new string[] {
                "[Default]",
                "MasterEnable = A:VAR1, number == 1",
                "HDG = A:VARA, number == 1",

                "[Aircraft.A1]",
                "HDG = A:VARB, number == 1",

                "[Aircraft.A2]",
                "MasterEnable = A:VAR2, number == 1",
                "NAV = A:VARC, number == 1"
            });

            var a1LightExpressions = LightExpressionConfig.ComputeLightExpressions(config, "A1");
            var a2LightExpressions = LightExpressionConfig.ComputeLightExpressions(config, "A2");

            // Inherited MasterEnable and overridden light
            Assert.Equal("((A:VAR1, number == 1) AND (A:VARB, number == 1))", FindLight(a1LightExpressions, "HDG").Expression.ToString());
            // Inherited light and overridden MasterEnable
            Assert.Equal("((A:VAR2, number == 1) AND (A:VARA, number == 1))", FindLight(a2LightExpressions, "HDG").Expression.ToString());
            // Overridden MasterEnable and overridden light
            Assert.Equal("((A:VAR2, number == 1) AND (A:VARC, number == 1))", FindLight(a2LightExpressions, "NAV").Expression.ToString());
        }

        [Fact]
        public void MasterEnableOptimizations()
        {
            var config = new FileConfig("Config.ini");
            config.LoadConfig(new string[] {
                "[Default]",
                "MasterEnable = A:VAR1, number == 1",
                "HDG = OFF",
                "NAV = ON",
                "APR = A:VARA,number == 1",

                "[Aircraft.A1]",
                "MasterEnable = ON"
            });

            var generalLightExpressions = LightExpressionConfig.ComputeLightExpressions(config, "General");
            var a1LightExpressions = LightExpressionConfig.ComputeLightExpressions(config, "A1");

            Assert.Equal("OFF", FindLight(generalLightExpressions, "HDG").Expression.ToString());
            Assert.Equal("(A:VAR1, number == 1)", FindLight(generalLightExpressions, "NAV").Expression.ToString());
            Assert.Equal("((A:VAR1, number == 1) AND (A:VARA, number == 1))", FindLight(generalLightExpressions, "APR").Expression.ToString());

            Assert.Equal("OFF", FindLight(a1LightExpressions, "HDG").Expression.ToString());
            Assert.Equal("ON", FindLight(a1LightExpressions, "NAV").Expression.ToString());
            Assert.Equal("(A:VARA, number == 1)", FindLight(a1LightExpressions, "APR").Expression.ToString());
        }

        [Fact]
        public void SupportsAircraftSpecificInversion()
        {
            var config = new FileConfig("Config.ini");
            config.LoadConfig(new string[]
            {
                "[Default]",
                "HDG = ON",
                "NAV = ON",
                "ALT = ON",
                "[Aircraft.AC1]",
                "Invert = NAV"
            });

            var lightExpressions = LightExpressionConfig.ComputeLightExpressions(config, "AC1");

            Assert.Equal("ON", FindLight(lightExpressions, "HDG").Expression.ToString());
            Assert.Equal("OFF", FindLight(lightExpressions, "NAV").Expression.ToString());
            Assert.Equal("ON", FindLight(lightExpressions, "ALT").Expression.ToString());
        }

        [Fact]
        public void InversionsApplyAfterMasterEnable()
        {
            var config = new FileConfig("Config.ini");
            config.LoadConfig(new string[]
            {
                "[Default]",
                "MasterEnable = A:VAR1, number == 1",
                "NAV = A:VAR2, number == 1",
                "Invert = NAV",
            });

            var lightExpressions = LightExpressionConfig.ComputeLightExpressions(config, "AC1");

            // If MasterEnable says that the light should be off, we don't THEN invert it and turn it on.
            Assert.Equal("((A:VAR1, number == 1) AND (NOT (A:VAR2, number == 1)))", FindLight(lightExpressions, "NAV").Expression.ToString());
        }

        private static LightExpression FindLight(IEnumerable<LightExpression> lightExpressions, string lightName)
        {
            return lightExpressions.Where(le => le.LightName == lightName).SingleOrDefault();
        }
    }
}
