using System;
using System.Diagnostics;
using System.IO;
using System.Timers;
using BravoLights.Installation;

namespace BravoLights
{
    public class Config
    {
        /// <summary>
        /// A cooloff timer to prevent us reading the config file as soon as it changes.
        /// </summary>
        private readonly Timer backoffTimer = new() { AutoReset = false, Interval = 100 };

        private FileSystemWatcher fsWatcher;

        private readonly IniFile iniFile = new();

        public Config()
        {
            backoffTimer.Elapsed += BackoffTimer_Elapsed;
        }


        public event EventHandler OnConfigChanged;

        public void Monitor()
        {
            fsWatcher = new FileSystemWatcher(Path.GetDirectoryName(ConfigIniPath));
            fsWatcher.Changed += ConfigFileChanged;
            fsWatcher.Created += ConfigFileChanged;
            fsWatcher.EnableRaisingEvents = true;

            ReadConfig();
        }

        private void BackoffTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            ReadConfig();
        }

        private void ConfigFileChanged(object sender, FileSystemEventArgs e)
        {
            if (e.FullPath == ConfigIniPath)
            {
                backoffTimer.Stop();
                backoffTimer.Start();
            }
        }

        private string ConfigIniPath
        {
            get
            {
                var baseFilename = "Config.ini";

                var candidates = new string[] {
                    Path.Combine(FlightSimulatorPaths.BetterBravoLightsPath, baseFilename),
                    Path.Combine(new DirectoryInfo(FlightSimulatorPaths.BetterBravoLightsPath).Parent.FullName, baseFilename)
                };

                foreach (var path in candidates)
                {
                    if (File.Exists(path))
                    {
                        return path;
                    }
                }

                return candidates[0];
            }
        }

        public string GetConfig(string aircraft, string key)
        {
            lock (this)
            {
                var value = iniFile.GetValueOrNull($"Aircraft.{aircraft}", key);
                if (value != null)
                {
                    return value;
                }

                value = iniFile.GetValueOrNull("Default", key);
                return value;
            }
        }

        private void ReadConfig()
        {
            Debug.WriteLine("Reading config file");
            try
            {
                lock (this)
                {
                    iniFile.LoadConfigFromFile(ConfigIniPath);
                }
                OnConfigChanged?.Invoke(this, EventArgs.Empty);
            }
            catch
            {
                Debug.WriteLine("Failed to read file");
                return;
            }
        }

        public void LoadConfig(string[] lines)
        {
            lock (this)
            {
                iniFile.LoadConfigLines(lines);
            }
        }
    }
}

