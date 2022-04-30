using System;
using System.IO;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

namespace BravoLights.Installation
{
    public class CorruptExeXmlException : Exception
    {
        public CorruptExeXmlException(string exeXmlPath, Exception innerException)
            : base("Existing exe.xml file is corrupt", innerException)
        {
            ExeXmlFilename = exeXmlPath;
            try
            {
                OriginalContent = File.ReadAllText(exeXmlPath);
                RepairedContent = ExeXmlFixer.TryFix(OriginalContent);
            }
            catch
            {
            }
        }

        public string ExeXmlFilename { get; private set; }

        public string OriginalContent { get; private set; }
        public string RepairedContent { get; private set; }

        public override string ToString()
        {
            return base.ToString();
        }
    }

    static class Installer
    {
        private static readonly string AFCBridgeAddonName = "AFCBridge";
        private static readonly string BravoLightsAddonName = "BravoLights";

        static Installer()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public static bool IsSetToRunOnStartup
        {
            get
            {
                try
                {
                    var xdoc = XDocument.Load(FlightSimulatorPaths.ExeXmlPath);
                    var lightsEl = FindAddon(xdoc, BravoLightsAddonName);
                    if (lightsEl != null)
                    {
                        return lightsEl.Element("Disabled")?.Value != "True";
                    }
                }
                catch
                {
                    // If we can't even find/read the exe.xml file, it's not going to be set to run on startup
                }

                return false;
            }
        }

        /// <summary>
        /// Tests if the exe.xml entry contains the current recommended entries.
        /// </summary>
        public static bool InstallationNeedsUpdating
        {
            get
            {
                try
                {
                    var xdoc = XDocument.Load(FlightSimulatorPaths.ExeXmlPath);
                    var lightsEl = FindAddon(xdoc, BravoLightsAddonName);
                    if (lightsEl != null)
                    {
                        return lightsEl.Element("ManualLoad") == null;
                    }
                }
                catch
                {
                }

                return false;
            }
        }

        private static XDocument LoadExeXml()
        {
            try
            {
                return XDocument.Load(FlightSimulatorPaths.ExeXmlPath);
            }
            catch (FileNotFoundException)
            {
                // The exe.xml file does not exist. Make one.
                var doc = new XDocument(
                    new XDeclaration("1.0", "windows-1252", null),
                    new XElement("SimBase.Document",
                        new XAttribute("Type", "SimConnect"),
                        new XAttribute("version", "1,0"),
                        new XElement("Descr", "Auto launch external applications on MSFS start"),
                        new XElement("Filename", "exe.xml"),
                        new XElement("Disabled", "False")
                        )
                    );
                return doc;
            }
            catch (XmlException ex)
            {
                throw new CorruptExeXmlException(FlightSimulatorPaths.ExeXmlPath, ex);
            }
        }

        public static string Install()
        {
            var xdoc = LoadExeXml();

            var afcBridgeEl = FindAddon(xdoc, AFCBridgeAddonName);
            if (afcBridgeEl != null)
            {
                afcBridgeEl.SetElementValue("Disabled", "True");
            }

            var lightsEl = FindAddon(xdoc, BravoLightsAddonName);
            if (lightsEl == null)
            {
                lightsEl = new XElement("Launch.Addon", new XElement("Name", BravoLightsAddonName));
                xdoc.Root.Add(lightsEl);
            }
            lightsEl.SetElementValue("Disabled", "False");
            lightsEl.SetElementValue("ManualLoad", "False");
            lightsEl.SetElementValue("Path", Path.Join(FlightSimulatorPaths.BetterBravoLightsPath, "BetterBravoLights.exe"));
            lightsEl.SetElementValue("CommandLine", "/startedbysimulator");
            lightsEl.SetElementValue("NewConsole", "False");

            xdoc.Save(FlightSimulatorPaths.ExeXmlPath);

            var message = new StringBuilder();

            if (afcBridgeEl == null)
            {
                message.AppendLine("Better Bravo Lights will now automatically start up with MSFS.");
            }
            else
            {
                message.AppendLine("Better Bravo Lights will now automatically start up with MSFS instead of the AFCBridge.");
            }

            InstallWasmModule();

            message.AppendLine();
            message.AppendLine($"Better Bravo Lights will run from {FlightSimulatorPaths.BetterBravoLightsPath}");

            message.AppendLine();
            message.AppendLine($"WASM module installed to {FlightSimulatorPaths.InstalledWasmModulePath}");

            if (new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
            {
                message.AppendLine();
                message.AppendLine($"(By the way, you don't need to run this installer as Administrator!)");
            }

            return message.ToString();
        }

        public static string Uninstall()
        {
            if (!File.Exists(FlightSimulatorPaths.ExeXmlPath))
            {
                // The file does not exist. No need to write out a version of exe.xml with our entry removed.
                return "Better Bravo Lights was not installed.";
            }

            var xdoc = LoadExeXml();

            var afcBridgeEl = FindAddon(xdoc, AFCBridgeAddonName);
            if (afcBridgeEl != null)
            {
                afcBridgeEl.SetElementValue("Disabled", "False");
            }

            var lightsEl = FindAddon(xdoc, BravoLightsAddonName);
            if (lightsEl != null)
            {
                lightsEl.Remove();
            }

            xdoc.Save(FlightSimulatorPaths.ExeXmlPath);

            var message = new StringBuilder();

            if (afcBridgeEl == null)
            {
                message.AppendLine("Better Bravo Lights will no longer start up with MSFS.");
            }
            else
            {
                message.AppendLine("Better Bravo Lights will no longer start up with MSFS. AFCBridge will now be used instead.");
            }

            if (InstalledWasmModuleVersion != null)
            {
                UninstallWasmModule();
                message.AppendLine();
                message.AppendLine($"WASM module uninstalled from {FlightSimulatorPaths.InstalledWasmModulePath}");
            }

            return message.ToString();
        }

        private static XElement FindAddon(XDocument document, string name)
        {
            foreach (var addonEl in document.Element("SimBase.Document").Elements("Launch.Addon"))
            {
                var addonNameElement = addonEl.Element("Name");
                if (addonNameElement != null)
                {
                    if (addonNameElement.Value == name)
                    {
                        return addonEl;
                    }
                }
            }
            return null;
        }

        public static string GetWasmModuleVersion(string modulePath)
        {
            var manifestPath = Path.Join(modulePath, "manifest.json");

            if (!File.Exists(manifestPath))
            {
                return null;
            }

            var text = File.ReadAllText(manifestPath);
            var doc = JsonDocument.Parse(text);
            return doc.RootElement.GetProperty("package_version").GetString();
        }

        public static string InstalledWasmModuleVersion
        {
            get
            {
                return GetWasmModuleVersion(FlightSimulatorPaths.InstalledWasmModulePath);
            }
        }

        public static string IncludedWasmModuleVersion
        {
            get
            {
                var version = GetWasmModuleVersion(FlightSimulatorPaths.IncludedWasmModulePath);
                if (version == null)
                {
                    throw new Exception("Missing included WASM module");
                }
                return version;
            }
        }

        public static void InstallWasmModule()
        {
            UninstallWasmModule();
            FileUtils.CopyDirectory(FlightSimulatorPaths.IncludedWasmModulePath, FlightSimulatorPaths.InstalledWasmModulePath);
        }

        public static void UninstallWasmModule()
        {
            FileUtils.RemoveDirectoryRecursively(FlightSimulatorPaths.InstalledWasmModulePath);
        }

        /// <summary>
        /// Tests whether MSFS's exe.xml file contains some entry for BBL. Doesn't test
        /// whether it's enabled or disabled.
        /// </summary>
        public static bool IsBBLMentionedInExeXml
        {
            get
            {
                try
                {
                    var exeXmlContents = File.ReadAllText(FlightSimulatorPaths.ExeXmlPath);
                    return exeXmlContents.Contains("BetterBravoLights.exe");
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether there's a problem with the installation configuration; this should
        /// be used when starting up the app normally, to check if something has been broken in the installation.
        /// </summary>
        public static string InstallationProblem
        {
            get
            {
                try
                {
                    var xdoc = LoadExeXml();
                    var lightsEl = FindAddon(xdoc, BravoLightsAddonName);
                    if (lightsEl != null)
                    {
                        var disabledElem = lightsEl.Element("Disabled");
                        if (disabledElem != null && disabledElem.Value == "True")
                        {
                            // It's installed but disabled. Don't bother checking the file path
                            return null;
                        }
                        var pathElem = lightsEl.Element("Path");
                        if (pathElem != null)
                        {
                            var installedBblPath = pathElem.Value;
                            if (!File.Exists(installedBblPath))
                            {
                                return $"BetterBravoLights was installed into Flight Simulator at the path\n{installedBblPath}\nbut that file no longer exists.\n\nAs a result, BetterBravoLights will not currently run automatically when MSFS starts.\n\nTo fix this, restore the original path or re-run the BBL installer from the new path.";
                            }
                        }
                    }
                }
                catch (CorruptExeXmlException)
                {
                    // Right now, the exe.xml file is corrupted. If we haven't been installed, that's okay.
                    if (IsBBLMentionedInExeXml)
                    {
                        // We're in a situation where we HAVE been installed previously but now the exe.xml file is
                        // corrupt. Presumably we've been started up manually because the user is trying to get
                        // BBL working.
                        return "After BetterBravoLights was installed, some other installation has broken the file that controls the programs that Flight Simulator launches when it starts.\n\nAs a result, BetterBravoLights (and some other programs) will not currently run automatically when MSFS starts.\n\nRun the BetterBravoLights installer and it will either automatically fix the problem or help you get it fixed.";
                    }
                }

                return null;
            }
        }
    }
}
