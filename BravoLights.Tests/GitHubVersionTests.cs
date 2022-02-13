using BravoLights.UI;
using System;
using System.IO;
using System.Reflection;
using Xunit;

namespace BravoLights.Tests
{
    public class GitHubVersionTests
    {

        [Fact]
        public void DetectsLatestVersionFromAnOutOfOrderAssetsList()
        {
            var codeBaseUrl = new Uri(Assembly.GetExecutingAssembly().Location);
            var codeBasePath = Uri.UnescapeDataString(codeBaseUrl.AbsolutePath);
            var dirPath = Path.GetDirectoryName(codeBasePath);
            var filename = Path.Combine(dirPath, "GitHubReleasesSample.json");
            var text = File.ReadAllText(filename);

            var latestVersion = ProgramInfo.ExtractLatestVersionFromGitHubReleasesJson(text);
            Assert.Equal("0.6.0", latestVersion);
        }
    }
}
