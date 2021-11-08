using System;
using System.Collections.Generic;

namespace BravoLights.Common.Ast
{
    /// <summary>
    /// Represents a node in a parsed expression tree.
    /// </summary>
    public interface IAstNode
    {
        string ErrorText { get; }

        event EventHandler<ValueChangedEventArgs> ValueChanged;

        IEnumerable<IVariable> Variables { get; }

        /// <summary>
        /// Gets the type of the overall value of this node.
        /// </summary>
        NodeDataType ValueType { get; }
    }
}
