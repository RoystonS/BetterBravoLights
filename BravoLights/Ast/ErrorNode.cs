using System;
using System.Collections.Generic;

namespace BravoLights.Ast
{
    class ErrorNode : IAstNode
    {
        public string ErrorText { get; set; }

        public IEnumerable<IVariable> Variables {
            get { return new IVariable[0]; }
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
