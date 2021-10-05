using System;
using System.Collections.Generic;
using System.Globalization;
using FSUIPC;

namespace BravoLights.Ast
{
    enum UICPCOffsetType
    {
        FLOAT, INT
    }
    class FSUIPCOffsetExpression : IAstNode
    {
        private readonly Offset<ushort> onGround = new(0x0366);         // 2-byte offset - Unsigned short

        private readonly Offset offset;
        private readonly UICPCOffsetType type;

        private FSUIPCOffsetExpression(int offset, int size, UICPCOffsetType type)
        {
            this.offset = new Offset(offset, size);
            this.type = type;
        }

        public string ErrorText { get { return null; } }

        public object Evaluate()
        {
            if (!FSUIPCConnection.IsOpen)
            {
                FSUIPCConnection.Open();
            }

            FSUIPCConnection.Process();

            if (this.type == UICPCOffsetType.FLOAT)
            {
                return offset.GetValue<double>();
            }
            else
            {
                return (double)offset.GetValue<ushort>();
            }
        }

        public IEnumerable<IVariable> Variables => throw new System.NotImplementedException();

        public static FSUIPCOffsetExpression Create(string text)
        {
            // OFFSET:[0-9A-F]+:(FLOAT | INT)[1248]
            var bits = text.Split(":");
            var offset = int.Parse(bits[1], NumberStyles.HexNumber);
            var sizeAndType = bits[2];
            UICPCOffsetType type;
            int size;
            if (sizeAndType.StartsWith("FLOAT"))
            {
                type = UICPCOffsetType.FLOAT;
                size = int.Parse(sizeAndType[5..]);
            }
            else
            {
                type = UICPCOffsetType.INT;
                size = int.Parse(sizeAndType[3..]);
            }

            // TODO: error handling

            return new FSUIPCOffsetExpression(offset, size, type);
        }

        event EventHandler<ValueChangedEventArgs> IAstNode.ValueChanged
        {
            add { }
            remove { }
        }
    }
}
