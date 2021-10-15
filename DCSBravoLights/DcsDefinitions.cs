using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace DCSBravoLights
{
    class DcsDefinitions : IEnumerable<DataDefinition>
    {
        private readonly Dictionary<VariableName, DataDefinition> definitions = new();
        private readonly ISet<DataDefinition>[] definitionsByAddress = new ISet<DataDefinition>[65536];

        public DcsDefinitions()
        {
            LoadDefinitionFile("MetadataStart");
            LoadDefinitionFile("MetadataEnd");
        }

        public void LoadDefinitionFile(string aliasName)
        {
            var commonDataJson = File.ReadAllText(Path.Join(DcsBiosState.DcsJsonDocFolder, aliasName + ".json"));

            var commonData = JsonDocument.Parse(commonDataJson);
            foreach (var rootElem in commonData.RootElement.EnumerateObject())
            {
                var name = rootElem.Name;
                foreach (var nextElem in rootElem.Value.EnumerateObject())
                {
                    var name2 = nextElem.Name;
                    var val = nextElem.Value;

                    var outputs = val.GetProperty("outputs");

                    var category = val.GetProperty("category").GetString();
                    var identifier = val.GetProperty("identifier").GetString();
                    string description;
                    try
                    {
                        description = val.GetProperty("description").GetString();
                    }
                    catch (KeyNotFoundException)
                    {
                        description = string.Empty;
                    }

                    foreach (var output in outputs.EnumerateArray())
                    {
                        if (outputs.GetArrayLength() > 1)
                        {
                            description += " - " + output.GetProperty("description").GetString();
                        }

                        var address = output.GetProperty("address").GetUInt16();
                        var suffix = output.GetProperty("suffix").GetString();

                        DataDefinition def;

                        if (output.GetProperty("type").GetString() == "integer")
                        {
                            def = new IntegerDefinition
                            {
                                VariableName = new VariableName(category, identifier),
                                Description = description,
                                Address = address,
                                Suffix = suffix,
                                Mask = output.GetProperty("mask").GetUInt16(),
                                MaxValue = output.GetProperty("max_value").GetUInt16(),
                                ShiftBy = output.GetProperty("shift_by").GetUInt16()
                            };
                        }
                        else
                        {
                            var maxLength = output.GetProperty("max_length").GetUInt16();
                            def = new StringDefinition
                            {
                                VariableName = new VariableName(category, identifier),
                                Description = description,
                                Address = address,
                                Suffix = suffix,
                                MaxLength = maxLength
                            };
                        }

                        AddDefinition(def);
                    }
                }
            }
        }

        public DataDefinition GetDataDefinition(VariableName variableName)
        {
            if (definitions.TryGetValue(variableName, out var def))
            {
                return def;
            } else
            {
                return null;
            }
        }

        private void AddDefinition(DataDefinition def)
        {
            definitions[def.VariableName] = def;

            for (var i = 0; i < def.Length; i++)
            {
                var set = definitionsByAddress[i + def.Address];
                if (set == null)
                {
                    set = new HashSet<DataDefinition>();
                    definitionsByAddress[i + def.Address] = set;
                }
                set.Add(def);
            }
        }

        public IEnumerator<DataDefinition> GetEnumerator()
        {
            return definitions.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
