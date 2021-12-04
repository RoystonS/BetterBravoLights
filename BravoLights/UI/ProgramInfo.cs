using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

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

        private static async Task<string> FetchLatestVersionStringAsync()
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("RoystonS-BetterBravoLights", VersionString));
            var response = await client.GetStringAsync("https://api.github.com/repos/RoystonS/BetterBravoLights/releases");
            var doc = JsonDocument.Parse(response);
            var firstReleaseEntry = doc.RootElement[0];

            // v0.6.0
            var releaseName = firstReleaseEntry.GetProperty("name").GetString();
            var versionString = releaseName[1..];
            return versionString;
        }

        private static Task<string> cachedLatestVersionFetch;

        public static Task<string> GetLatestVersionStringAsync()
        {
            if (cachedLatestVersionFetch == null)
            {
                cachedLatestVersionFetch = FetchLatestVersionStringAsync();
            }

            return cachedLatestVersionFetch;
        }

        public static async Task<bool> IsNewVersionAvailableAsync()
        {
            try
            {
                var latestVersion = await GetLatestVersionStringAsync();
                return latestVersion != VersionString;
            } catch
            {
                return false;
            }
        }
    }
}
