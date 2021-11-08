using System;
using System.Collections.Generic;

namespace BravoLights.Common.Ast
{
    public class ErrorNode : IAstNode
    {
        public ErrorNode(string errorText)
        {
            ErrorText = errorText;
        }

        public string ErrorText { get; private set; }

        public IEnumerable<IVariable> Variables {
            get { return Array.Empty<IVariable>(); }
        }

        public NodeDataType ValueType
        {
            get { return NodeDataType.Double; }
        }

        event EventHandler<ValueChangedEventArgs> IAstNode.ValueChanged
        {
            add { }
            remove { }
        }

        public override string ToString()
        {
            return ErrorText;
        }
    }
}
