using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using BravoLights.Common;

namespace DCSBravoLights
{
    /// <summary>
    /// Manages subscriptions to variables by NAME. 
    /// </summary>
    /// <remarks>
    /// In order to cope with different aircraft having duplicate variable names,
    /// this class tracks the current aircraft and ensures that the correct
    /// offset is subscribed for each variable for the current aircraft.
    /// </remarks>
    public class DcsVariablesManager
    {
        public readonly DcsBiosState DcsBiosState;

        private readonly Dictionary<VariableName, EventHandler<ValueChangedEventArgs>> allHandlers = new();
        private DcsDefinitions currentDefinitions;

        public DcsVariablesManager(DcsBiosState dcsBiosState)
        {
            this.DcsBiosState = dcsBiosState;

            LoadAircraftAliases();

            currentDefinitions = new DcsDefinitions();

            var aircraftNameDefinition = currentDefinitions.GetDataDefinition(new VariableName("Metadata", "_ACFT_NAME"));
            dcsBiosState.AddHandler(aircraftNameDefinition, HandleAircraftNameChanged);
        }

        public event EventHandler AircraftNameChanged;
        public event EventHandler DefinitionsChanged;

        public static string GetNullTerminatedString(string str)
        {
            StringBuilder builder = new();
            foreach (var ch in str)
            {
                if (((short)ch) == 0)
                {
                    return builder.ToString();
                }
                builder.Append(ch);
            }
            return builder.ToString();
        }

        private static readonly ValueChangedEventArgs VariableNotKnownToCurrentAircraftEventArgs = new()
        {
            NewValue = new Exception("Value not supported by current aircraft")
        };

        private static void SendVariableNotKnownToCurrentAircraft(EventHandler<ValueChangedEventArgs> handlers)
        {
            handlers(null, VariableNotKnownToCurrentAircraftEventArgs);
        }

        private string aircraftName = "";
        public string AircraftName
        {
            get
            {
                return aircraftName;
            }
        }

        public IEnumerable<DataDefinition> DataDefinitions
        {
            get
            {
                return currentDefinitions;
            }
        }


        private void HandleAircraftNameChanged(object def, ValueChangedEventArgs e)
        {
            var aircraftName = "";

            if (e.NewValue is not Exception)
            {
                aircraftName = GetNullTerminatedString((string)e.NewValue);
            }

            if (aircraftName == this.AircraftName)
            {
                // Nothing to do
                return;
            }

            if (!aircraftAliases.TryGetValue(aircraftName, out var aliases))
            {
                // Unknown aircraft.
                // TODO: report
                aliases = aircraftAliases[""];
            }

            // Unsubscribe old definitions
            foreach (var kvp in allHandlers)
            {
                var variableName = kvp.Key;
                var handlers = kvp.Value;

                var oldDefn = currentDefinitions.GetDataDefinition(variableName);
                if (oldDefn != null)
                {
                    DcsBiosState.RemoveHandler(oldDefn, HandleVariableChanged);
                }

                SendVariableNotKnownToCurrentAircraft(handlers);
            }

            currentDefinitions = new DcsDefinitions();
            foreach (var definitionFile in aliases)
            {
                currentDefinitions.LoadDefinitionFile(definitionFile);
            }

            foreach (var variableName in allHandlers.Keys)
            {
                var newDefn = currentDefinitions.GetDataDefinition(variableName);
                if (newDefn != null)
                {
                    DcsBiosState.AddHandler(newDefn, HandleVariableChanged);
                }
            }

            this.aircraftName = aircraftName;
            AircraftNameChanged?.Invoke(this, EventArgs.Empty);
            DefinitionsChanged?.Invoke(this, EventArgs.Empty);
        }

        public void AddHandler(VariableName variableName, EventHandler<ValueChangedEventArgs> handler)
        {
            allHandlers.TryGetValue(variableName, out var existingHandlers);
            var newHandlers = (EventHandler<ValueChangedEventArgs>)Delegate.Combine(existingHandlers, handler);
            allHandlers[variableName] = newHandlers;

            var def = currentDefinitions.GetDataDefinition(variableName);
            if (def != null)
            {
                if (existingHandlers == null)
                {
                    // This will send out a notification to the new handler
                    DcsBiosState.AddHandler(def, HandleVariableChanged);
                } else
                {
                    var existingValue = DcsBiosState.GetValue(def);
                    handler(def, new ValueChangedEventArgs { NewValue = existingValue });
                }
            } else
            {
                SendVariableNotKnownToCurrentAircraft(handler);
            }
        }

        public void RemoveHandler(VariableName variableName, EventHandler<ValueChangedEventArgs> handler)
        {
            allHandlers.TryGetValue(variableName, out var existingHandlers);
            var newHandlers = (EventHandler<ValueChangedEventArgs>)Delegate.Remove(existingHandlers, handler);
            allHandlers[variableName] = newHandlers;

            var def = currentDefinitions.GetDataDefinition(variableName);
            if (def != null)
            {
                if (newHandlers == null)
                {
                    DcsBiosState.RemoveHandler(def, HandleVariableChanged);
                }
            }
        }

        public object GetValue(VariableName variableName)
        {
            var def = currentDefinitions.GetDataDefinition(variableName);
            if (def != null)
            {
                return DcsBiosState.GetValue(def);
            }

            return null;
        }

        private void HandleVariableChanged(object def, ValueChangedEventArgs e)
        {
            var defn = (DataDefinition)def;
            var handlers = allHandlers[defn.VariableName];
            handlers(def, e);
        }

        private readonly Dictionary<string, string[]> aircraftAliases = new();

        private void LoadAircraftAliases()
        {
            var aliasesJson = File.ReadAllText(Path.Join(DcsBiosState.DcsJsonDocFolder, "AircraftAliases.json"));
            var doc = JsonDocument.Parse(aliasesJson);

            // Document is a map from aircraft name to an array of aircraft files to load
            foreach (var rootElem in doc.RootElement.EnumerateObject())
            {
                var aircraftName = rootElem.Name;
                var filesToLoad = rootElem.Value.EnumerateArray().Select(x => x.GetString()).ToArray();
                aircraftAliases[aircraftName] = filesToLoad;
            }
        }
    }
}
