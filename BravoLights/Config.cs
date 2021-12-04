using System;
using System.Diagnostics;
using System.IO;
using System.Timers;

namespace BravoLights
{
    public interface IConfig
    {
        string GetConfig(string aircraft, string key);
        event EventHandler OnConfigChanged;
    }

    public class FileConfig : IConfig
    {
        /// <summary>
        /// A cooloff timer to prevent us reading the config file as soon as it changes.
        /// </summary>
        private readonly Timer backoffTimer = new() { AutoReset = false, Interval = 100 };

        private FileSystemWatcher fsWatcher;

        private readonly IniFile iniFile = new();
        private readonly string filePath;

        public FileConfig(string filePath)
        {
            backoffTimer.Elapsed += BackoffTimer_Elapsed;
            this.filePath = Path.GetFullPath(filePath);
        }


        public event EventHandler OnConfigChanged;

        public void Monitor()
        {
            fsWatcher = new FileSystemWatcher(Path.GetDirectoryName(filePath));
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
            if (e.FullPath == filePath)
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
                    iniFile.LoadConfigFromFile(filePath);
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

    public class ConfigChain : IConfig
    {
        private readonly IConfig[] configs;

        public ConfigChain(params IConfig[] configs)
        {
            this.configs = configs;
        }

        public event EventHandler OnConfigChanged
        {
            add
            {
                foreach (var config in configs)
                {
                    config.OnConfigChanged += value;
                }
            }
            remove
            {
                foreach (var config in configs)
                {
                    config.OnConfigChanged -= value;
                }
            }
        }

        public string GetConfig(string aircraft, string key)
        {
            foreach (var config in configs)
            {
                var result = config.GetConfig(aircraft, key);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }
}

