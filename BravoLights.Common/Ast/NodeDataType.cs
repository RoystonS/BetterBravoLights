using System;

namespace BravoLights.Common.Ast
{
    public enum NodeDataType
    {
        /// <summary>
        /// Indicates that the node exposes a value of type Double.
        /// </summary>
        Double,

        /// <summary>
        /// Indicates that the node exposes a value of type Boolean.
        /// </summary>
        Boolean
    }

    public static class NodeDataTypeUtility
    {
        public static NodeDataType GetNodeDataType(Type valueType)
        {
            if (valueType ==typeof(bool))
            {
                return NodeDataType.Boolean;
            }
            if (valueType == typeof(double))
            {
                return NodeDataType.Double;
            }

            throw new Exception($"Unexpected node value type: {valueType.Name}");
        }
    }
}
