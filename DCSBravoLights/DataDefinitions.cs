using System.Text;

namespace DCSBravoLights
{
    public enum DataType
    {
        Integer,
        String
    }

    public abstract class DataDefinition
    {
        public VariableName VariableName { get; set; }

        public string Description { get; set; }

        public string Suffix { get; set; }

        public ushort Address { get; set; }

        public abstract DataType Type { get; }

        public abstract ushort Length { get; }

        public abstract object GetValue(byte[] rawData, bool[] dataValid);

        public abstract string GetStringValue(byte[] rawData, bool[] dataValid);

        public abstract bool WouldChange(byte[] oldData, ushort position, byte newByte);
    }

    public class IntegerDefinition : DataDefinition
    {
        public override DataType Type { get { return DataType.Integer; } }
        public override ushort Length { get { return 2; } }

        public ushort Mask;
        public ushort MaxValue;
        public ushort ShiftBy;

        public override object GetValue(byte[] rawData, bool[] dataValid)
        {
            if (!dataValid[Address] || !dataValid[Address + 1])
            {
                return null;
            }
            var byte1 = rawData[Address];
            var byte2 = rawData[Address + 1];

            return Compute(byte1, byte2);
        }

        private ushort Compute(byte byte1, byte byte2)
        {
            var rawValue = (ushort)(byte1 + byte2 * 256);
            var masked = (ushort)(rawValue & Mask);
            var shifted = (ushort)(masked >> ShiftBy);
            return shifted;
        }

        public override bool WouldChange(byte[] oldData, ushort position, byte newByte)
        {
            var oldByte1 = oldData[Address];
            var oldByte2 = oldData[Address + 1];

            var oldValue = Compute(oldByte1, oldByte2);

            var newByte1 = oldByte1;
            var newByte2 = oldByte2;
            if (position == Address)
            {
                newByte1 = newByte;
            }
            else
            {
                newByte2 = newByte;
            }
            var newValue = Compute(newByte1, newByte2);
            return oldValue != newValue;
        }

        public override string GetStringValue(byte[] rawData, bool[] dataValid)
        {
            return GetValue(rawData, dataValid).ToString();
        }
    }

    public class StringDefinition : DataDefinition
    {
        public override DataType Type { get { return DataType.String; } }
        public ushort MaxLength;
        public override ushort Length { get { return MaxLength; } }

        public override string GetValue(byte[] rawData, bool[] dataValid)
        {
            for (var i = Address; i < Address + Length; i++)
            {
                if (!dataValid[i])
                {
                    return null;
                }
            }

            var subset = rawData[Address..(Address + Length)];
            return Encoding.ASCII.GetString(subset);
        }

        public override string GetStringValue(byte[] rawData, bool[] dataValid)
        {
            return GetValue(rawData, dataValid);
        }

        public override bool WouldChange(byte[] oldData, ushort position, byte newByte)
        {
            return oldData[position] != newByte;
        }
    }
}
