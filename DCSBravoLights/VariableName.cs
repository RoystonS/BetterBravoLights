using System;

namespace DCSBravoLights
{
    public class VariableName : IEquatable<VariableName>
    {
        private readonly string dcsCategory;
        private readonly string dcsIdentifier;

        public VariableName(string category, string identifier)
        {
            dcsCategory = category;
            dcsIdentifier = identifier;
        }

        public string DcsCategory { get => dcsCategory; }
        public string DcsIdentifier { get => dcsIdentifier; }

        public bool Equals(VariableName other)
        {
            return dcsCategory.Equals(other.dcsCategory) && dcsIdentifier.Equals(other.dcsIdentifier);
        }
        public override bool Equals(object obj)
        {
            return Equals(obj as VariableName);
        }

        public override int GetHashCode()
        {
            return dcsCategory.GetHashCode() + dcsIdentifier.GetHashCode();
        }
    }
}
