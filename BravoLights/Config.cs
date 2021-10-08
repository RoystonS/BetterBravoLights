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


        private static readonly Regex sectionRegex = new("\\[(.*)\\]");
        private static readonly Regex keyValueRegex = new("^(.*?)\\s*=\\s*(.*)$");

        public string GetConfig(string aircraft, string key)
        {
            if (sections.TryGetValue($"Aircraft.{aircraft}", out var section))
            {
                if (section.TryGetValue(key, out var value))
                {
                    return value;
                }
            }

            if (sections.TryGetValue("Default", out section))
            {
                if (section.TryGetValue(key, out string value))
                {
                    return value;
                }
            }

            return null;
        }

        private Dictionary<string, IniSection> sections = new();

        private void ReadConfig()
        {
            Debug.WriteLine("Reading config file");

            string[] configLines;

            try
            {
                configLines = File.ReadAllLines(Path.Join(System.Windows.Forms.Application.StartupPath, "Config.ini"));
                LoadConfig(configLines);
            }
            catch
            {
                Debug.WriteLine("Failed to read file");
                return;
            }
        }

        public void LoadConfig(string[] configLines)
        {
            ICollection<IniSection> currentSections = new List<IniSection>();

            var sections = new Dictionary<string, IniSection>();

            foreach (var rawLine in configLines)
            {
                var line = rawLine.Trim();

                if (line.StartsWith(";"))
                {
                    // Comment
                    continue;
                }

                if (line.Length == 0) {
                    // Empty line
                    continue;
                }

                var sectionMatch = sectionRegex.Match(line);
                if (sectionMatch.Success)
                {
                    var sectionNamesString = sectionMatch.Groups[1].Value;
                    var sectionNames = sectionNamesString.Split(',');
                    currentSections.Clear();

                    foreach (var sectionName in sectionNames)
                    {
                        var trimmedSectionName = sectionName.Trim();
                        if (!sections.TryGetValue(trimmedSectionName, out IniSection section))
                        {
                            section = new IniSection();
                            sections[trimmedSectionName] = section;
                        }
                        currentSections.Add(section);
                    }
                    continue;
                }


                var keyValueMatch = keyValueRegex.Match(line);
                if (keyValueMatch.Success)
                {
                    var key = keyValueMatch.Groups[1].Value;
                    var value = keyValueMatch.Groups[2].Value;
                    foreach (var section in currentSections)
                    {
                        section.Set(key, value);
                    }
                }
            }

            this.sections = sections;

            OnConfigChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    class IniSection
    {
        private readonly Dictionary<string, string> storage = new();

        public IniSection()
        {
        }
        
        
        public void Set(string key, string value)
        {
            storage[key] = value;
        }

        public bool TryGetValue(string key, out string value)
        {
            return storage.TryGetValue(key, out value);
        }
    }
}

