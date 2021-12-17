using System.Collections.Generic;
using System.Linq;
using BravoLights.Ast;
using BravoLights.Common;

namespace BravoLights
{
    internal class LightExpressionConfig
    {
        /// <summary>
        /// Computes the light expressions for a particular aircraft.
        /// </summary>
        /// <remarks>
        /// Returns an expression for every known light.
        /// </remarks>
        public static IEnumerable<LightExpression> ComputeLightExpressions(IConfig config, string aircraft)
        {
            var invert = config.GetConfig(aircraft, "Invert") ?? "";
            var masterEnable = config.GetConfig(aircraft, "MasterEnable") ?? "ON";

            var lightNamesToInvert = new HashSet<string>(invert.Split(',', ' ').Select(n => n.Trim()));

            var lightExpressions = LightNames.AllNames.Select(lightName =>
            {
                var expressionText = config.GetConfig(aircraft, lightName);
                if (expressionText == null)
                {
                    expressionText = "OFF";
                }

                if (lightNamesToInvert.Contains(lightName))
                {
                    expressionText = $"NOT({expressionText})";
                }

                expressionText = $"({masterEnable}) AND ({expressionText})";

                var expression = MSFSExpressionParser.Parse(expressionText).Optimize();

                return new LightExpression(lightName, expression, true);
            });

            return lightExpressions;
        }
    }
}
