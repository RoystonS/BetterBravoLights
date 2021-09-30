using System.Reflection;

namespace BravoLights.UI
{
    public class ProgramInfo
    {
        public static string ProductNameAndVersion
        {
            get
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                return $"Better Bravo Lights {version.Major}.{version.Minor}.{version.Build}";
            }
        }
    }
}
