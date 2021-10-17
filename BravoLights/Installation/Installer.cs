using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;

namespace BravoLights.Installation
{
    public class CorruptExeXmlException : Exception
    {
        private readonly string exeXmlPath;

        public CorruptExeXmlException(string exeXmlPath, Exception innerException)
            :base("Existing exe.xml file is corrupt", innerException)
        {
            this.exeXmlPath = exeXmlPath;
        }

        public string ExeXmlPath
        {
            get
            {
                return exeXmlPath;
            }
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

        /// <summary>
        /// Gets the location of the MSFS exe.xml file, which may not actually exist yet.
        /// </summary>
        private static string ExeXmlPath
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
                    var exeXmlPath = Path.Join(path, "exe.xml");

                    if (File.Exists(exeXmlPath))
                    {
                        return exeXmlPath;
                    }

                    if (File.Exists(Path.Join(path, "FlightSimulator.CFG")))
                    {
                        // There's no existing exe.xml file but it does belong here.
                        return exeXmlPath;
                    }
                }

                var pathsTried = String.Join(", ", pathsToTry);
                throw new Exception($"Could not locate exe.xml file. Paths tried: {pathsTried}");
            }
        }

        public static bool IsSetToRunOnStartup
        {
            get
            {
                try
                {
                    var xdoc = XDocument.Load(ExeXmlPath);
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
                    var xdoc = XDocument.Load(ExeXmlPath);
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
                return XDocument.Load(ExeXmlPath);
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
                throw new CorruptExeXmlException(ExeXmlPath, ex);
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
            lightsEl.SetElementValue("Path", Path.Join(Application.StartupPath, "BetterBravoLights.exe"));
            lightsEl.SetElementValue("CommandLine", "/startedbysimulator");
            lightsEl.SetElementValue("NewConsole", "False");

            xdoc.Save(ExeXmlPath);

            if (afcBridgeEl == null)
            {
                return "Better Bravo Lights will now automatically start up with MSFS.";
            } else
            {
                return "Better Bravo Lights will now automatically start up with MSFS instead of the AFCBridge.";
            }
        }

        public static string Uninstall()
        {
            if (!File.Exists(ExeXmlPath))
            {
                // The file does not exist. No need to write out a version with our entry removed.
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

            xdoc.Save(ExeXmlPath);

            if (afcBridgeEl == null)
            {
                return "Better Bravo Lights removed.";
            }
            else
            {
                return "Better Bravo Lights removed. AFCBridge will now be used instead.";
            }
        }

        private static XElement FindAddon(XDocument document, string name)
        {
            foreach (var addonEl in document.Element("SimBase.Document").Elements("Launch.Addon"))
            {
                var addonName = addonEl.Element("Name").Value;
                if (addonName == name)
                {
                    return addonEl;
                }
            }
            return null;
        }
    }
}
