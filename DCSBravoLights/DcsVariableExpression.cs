using BravoLights.Common;
using BravoLights.Common.Ast;

namespace DCSBravoLights
{
    class DcsVariableExpression : VariableBase, IAstNode
    {
        private readonly VariableName variableName;

        public DcsVariableExpression(string category, string identifier)
        {
            variableName = new VariableName(category, identifier);
        }

        public override string ToString()
        {
            return $"[{variableName.DcsCategory}:{variableName.DcsIdentifier}]";
        }

        protected override IConnection Connection => DcsConnection.Connection;

        public override string Identifier => $"{variableName.DcsCategory}:{variableName.DcsIdentifier}";

        public VariableName VariableName {  get { return variableName; } }
    }
}
