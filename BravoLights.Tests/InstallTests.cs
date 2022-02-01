using System;
using System.IO;
using Xunit;

namespace BravoLights.Tests
{
    public class InstallTests
    {
        [Fact]
        public void CopesWithNamelessAddons()
        {
            using (var paths = BuildTestFileSystem())
            {
                var steamFolder = paths.SteamPath;

                Directory.CreateDirectory(steamFolder);
                File.WriteAllText(Path.Join(steamFolder, "UserCfg.opt"), $"InstalledPackagesPath \"{paths.FSDataPath}\"");
                File.WriteAllText(Path.Join(steamFolder, "exe.xml"),
                    @"<?xml version=""1.0"" encoding=""Windows-1252""?>
<SimBase.Document Type=""SimConnect"" version=""1,0"">
    <Launch.Addon>
    </Launch.Addon>
</SimBase.Document>
");

                var message = Installation.Installer.Install();
                Assert.StartsWith("Better Bravo Lights will now automatically start up with MSFS.", message);
            }
        }


        [Fact]
        public void DisablesAFCBridgeAtInstall()
        {
            using (var paths = BuildTestFileSystem())
            {
                var steamFolder = paths.SteamPath;

                Directory.CreateDirectory(steamFolder);
                File.WriteAllText(Path.Join(steamFolder, "UserCfg.opt"), $"InstalledPackagesPath \"{paths.FSDataPath}\"");
                File.WriteAllText(Path.Join(steamFolder, "exe.xml"),
                    @"<?xml version=""1.0"" encoding=""Windows-1252""?>
<SimBase.Document Type=""SimConnect"" version=""1,0"">
  <Launch.Addon>
    <Name>AFCBridge</Name>
    <Disabled>False</Disabled>
    <Path>C:\Users\owner\AppData\Roaming\Microsoft Flight Simulator\Packages\community\AFC_Bridge\bin\AFC_Bridge.exe</Path>
  </Launch.Addon>
</SimBase.Document>
");
                var message = Installation.Installer.Install();
                Assert.StartsWith("Better Bravo Lights will now automatically start up with MSFS instead of the AFCBridge.", message);

                var finalXml = File.ReadAllText(Path.Join(steamFolder, "exe.xml"));
                Assert.Contains(@"
  <Launch.Addon>
    <Name>AFCBridge</Name>
    <Disabled>True</Disabled>", finalXml);
            }
        }

        private static string GetTemporaryDirectory()
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }

        private static Paths BuildTestFileSystem()
        {
            var folder = GetTemporaryDirectory();

            Installation.FlightSimulatorPaths.UnitTestRoot = folder;

            var windowsStoreFolder = Path.Join(folder, "LOCALAPPDATA", "Packages", "Microsoft.FlightSimulator_8wekyb3d8bbwe", "LocalCache");
            var steamFolder = Path.Join(folder, "APPDATA", "Microsoft Flight Simulator");

            var fsDataFolder = Path.Join(folder, "FSDATA");
            Directory.CreateDirectory(fsDataFolder);
            Installation.FlightSimulatorPaths.UnitTestRoot = folder;

            return new Paths
            {
                StorePath = windowsStoreFolder,
                SteamPath = steamFolder,
                FSDataPath = fsDataFolder,
                Disposer = () => {
                    Installation.FlightSimulatorPaths.UnitTestRoot = null;

                    Directory.Delete(folder, true); }
            };
        }
    }


    internal delegate void Disposer();

        class Paths : IDisposable
        {
            public Disposer Disposer { get; set; }

            public string StorePath { get; set; }
            public string SteamPath { get; set; }
            public string FSDataPath { get; set; }

        void IDisposable.Dispose()
        {
            this.Disposer();
        }
    }
}
