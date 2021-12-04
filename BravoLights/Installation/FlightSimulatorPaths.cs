using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace BravoLights.Installation
{
    static class FlightSimulatorPaths
    {
        /// <summary>
        /// Gets the location of the main Flight Simulator installation.
        /// </summary>
        public static string FlightSimulatorPath
        {
            get
            {
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var windowsStoreLocation = Path.Join(localAppData, "Packages", "Microsoft.FlightSimulator_8wekyb3d8bbwe", "LocalCache");

                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var steamLocation = Path.Join(appData, "Microsoft Flight Simulator");

                var pathsToTry = new[]
                {
                    windowsStoreLocation,
                    steamLocation
                };

                foreach (var path in pathsToTry)
                {
                    if (File.Exists(Path.Join(path, "FlightSimulator.CFG")) || File.Exists(Path.Join(path, "UserCfg.opt")))
                    {
                        return path;
                    }
                }

                var pathsTried = String.Join(", ", pathsToTry);
                throw new Exception($"Could not locate main Flight Simulator path. Paths tried: {pathsTried}");
            }
        }

        public static string BetterBravoLightsPath
        {
            get
            {
                return Application.StartupPath;
            }
        }

        /// <summary>
        /// Gets the location of the MSFS exe.xml file, which may not actually exist yet.
        /// </summary>
        public static string ExeXmlPath
        {
            get
            {
                return Path.Join(FlightSimulatorPath, "exe.xml");
            }
        }

        /// <summary>
        /// Gets the location of the UserCfg.opt file.
        /// </summary>
        private static string UserCfgOptPath
        {
            get
            {
                return Path.Join(FlightSimulatorPath, "UserCfg.opt");
            }
        }

        private static readonly Regex installedPackagesPathRegex = new("^InstalledPackagesPath \"(.*)\"");

        /// <summary>
        /// Gets the location of the Official and Community directories.
        /// </summary>
        public static string MSFSPackagesPath
        {
            get
            {
                var lines = File.ReadAllLines(UserCfgOptPath);
                foreach (var line in lines)
                {
                    var match = installedPackagesPathRegex.Match(line);
                    if (match.Success)
                    {
                        return match.Groups[1].Value;
                    }
                }

                throw new Exception("Cannot locate FS packages path");
            }
        }


        /// <summary>
        /// Gets the location of the Community directory.
        /// </summary>
        public static string CommunityPath
        {
            get { return Path.Join(MSFSPackagesPath, "Community"); }
        }


        private const string WasmModuleName = "better-bravo-lights-lvar-module";

        public static string InstalledWasmModulePath
        {
            get { return Path.Join(CommunityPath, WasmModuleName); }
        }
        public static string IncludedWasmModulePath
        {
            get { return Path.Join(BetterBravoLightsPath, WasmModuleName); }
        }

        public static string BuiltInConfigIniPath
        {
            get { return Path.Join(BetterBravoLightsPath, "Config.BuiltIn.ini"); }
        }

        public static string UserRuntimePath
        {
            get
            {
                return Path.Combine(new DirectoryInfo(BetterBravoLightsPath).Parent.FullName);
            }
        }
        public static string UserConfigIniPath
        {
            get
            {
                return Path.Combine(UserRuntimePath, "Config.ini");
            }
        }
    }
}
