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

                paths.EstablishAsMSFSRoot(steamFolder);
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

                paths.EstablishAsMSFSRoot(steamFolder);
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

        [Fact]
        public void CanComplainAboutCorruptExeXml()
        {
            using (var paths = BuildTestFileSystem())
            {
                var appFolder = paths.StorePath;

                paths.EstablishAsMSFSRoot(appFolder);

                // Establish an exe.xml file that contains our 
                File.WriteAllText(Path.Join(appFolder, "exe.xml"),
                    @"<?xml version=""1.0"" encoding=""windows-1252""?>
<Descr>Auto launch external applications on MSFS start</Descr>
<Filename>exe.xml</Filename>
<Disabled>False</Disabled>
<Name>BravoLights</Name>
<Path>C:\Users\royston\OneDrive\Desktop\BetterBravoLights WtiJo\Program\BetterBravoLights.exe</Path>
<CommandLine>/startedbysimulator</CommandLine>
<NewConsole>False</ NewConsole>
</Launch.Addon>
</SimBase.Document>");

                var message = Installation.Installer.InstallationProblem;
                Assert.Contains("BetterBravoLights was installed", message);
            }
        }

        private void EstablishLegalBBLExeXml(Paths paths, string appFolder, string supposedBblExecPath, bool bblDisabled)
        {
            paths.EstablishAsMSFSRoot(appFolder);

            File.WriteAllText(Path.Join(appFolder, "exe.xml"),
                $@"<?xml version=""1.0"" encoding=""windows-1252""?>
<SimBase.Document Type=""SimConnect"" version=""1,0"">
  <Descr>Auto launch external applications on MSFS start</Descr>
  <Filename>exe.xml</Filename>
  <Launch.Addon>
    <Disabled>{bblDisabled}</Disabled>
    <Name>BravoLights</Name>
    <Path>{supposedBblExecPath}</Path>
    <CommandLine>/startedbysimulator</CommandLine>
    <NewConsole>False</NewConsole>
  </Launch.Addon>
</SimBase.Document>");
        }

        [Fact]
        public void ComplainsAboutMissingInstallationLocation()
        {
            using (var paths = BuildTestFileSystem())
            {
                var supposedBblExecPath = Path.Join(paths.TestRoot, "Somewhere\\BetterBravoLights.exe");

                var appFolder = paths.StorePath;

                EstablishLegalBBLExeXml(paths, appFolder, supposedBblExecPath, false);

                var message = Installation.Installer.InstallationProblem;
                Assert.Contains("BetterBravoLights was installed", message);
                Assert.Contains(supposedBblExecPath, message);
                Assert.Contains("file no longer exists", message);
            }
        }

        [Fact]
        public void DoesNotComplainAboutMissingInstallationLocationIfBBLIsDisabled()
        {
            using (var paths = BuildTestFileSystem())
            {
                var supposedBblExecPath = Path.Join(paths.TestRoot, "Somewhere\\BetterBravoLights.exe");

                var appFolder = paths.StorePath;

                EstablishLegalBBLExeXml(paths, appFolder, supposedBblExecPath, true);

                Assert.Null(Installation.Installer.InstallationProblem);
            }
        }

        [Fact]
        public void DoesNotComplainAboutInstallationLocationIfNotMissing()
        {
            using (var paths = BuildTestFileSystem())
            {
                var supposedBblExecPath = Path.Join(paths.TestRoot, "Somewhere\\BetterBravoLights.exe");

                var appFolder = paths.StorePath;

                EstablishLegalBBLExeXml(paths, appFolder, supposedBblExecPath, true);

                // Create a dummy BBL exe file; that should be enough to satisfy the installation check
                Directory.CreateDirectory(Path.GetDirectoryName(supposedBblExecPath));
                File.WriteAllBytes(supposedBblExecPath, new byte[] { 1, 2, 3, 4, 5 });

                Assert.Null(Installation.Installer.InstallationProblem);
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
                TestRoot = folder,
                StorePath = windowsStoreFolder,
                SteamPath = steamFolder,
                FSDataPath = fsDataFolder,
                Disposer = () =>
                {
                    Installation.FlightSimulatorPaths.UnitTestRoot = null;

                    Directory.Delete(folder, true);
                }
            };
        }
    }


    internal delegate void Disposer();

    class Paths : IDisposable
    {
        public Disposer Disposer { get; set; }

        public string TestRoot { get; set; }
        public string StorePath { get; set; }
        public string SteamPath { get; set; }
        public string FSDataPath { get; set; }

        public void EstablishAsMSFSRoot(string folder)
        {
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Join(folder, "UserCfg.opt"), $"InstalledPackagesPath \"{FSDataPath}\"");
        }

        void IDisposable.Dispose()
        {
            this.Disposer();
        }
    }
}