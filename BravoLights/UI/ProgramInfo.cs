using System.Reflection;

namespace BravoLights.UI
{
    public class ProgramInfo
    {
        public static string ProductNameAndVersion
        {
            get
            {
                return $"Better Bravo Lights {VersionString}";
            }
        }

        public static string VersionString
        {
            get {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                return $"{version.Major}.{version.Minor}.{version.Build}";
            }
        }
    }
}
