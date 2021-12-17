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

        /// <summary>
        /// Calculates an optimized version of this expression, e.g. converting "A AND ON" to "A" and "A OR ON" to "ON".
        /// </summary>
        IAstNode Optimize();
    }
}
