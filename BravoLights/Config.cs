using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Timers;

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
            fsWatcher = new FileSystemWatcher(System.Windows.Forms.Application.StartupPath);
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
            if (e.FullPath.Contains("Config.ini", StringComparison.InvariantCultureIgnoreCase))
            {
                backoffTimer.Stop();
                backoffTimer.Start();
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
                    iniFile.LoadConfigFromFile(Path.Join(System.Windows.Forms.Application.StartupPath, "Config.ini"));
                }
                OnConfigChanged?.Invoke(this, EventArgs.Empty);
            }
            catch
            {
                Debug.WriteLine("Failed to read file");
                return;
            }
        }

    }

}

