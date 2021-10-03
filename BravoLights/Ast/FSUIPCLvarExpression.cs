using BravoLights.Connections;

namespace BravoLights.Ast
{
    class FSUIPCLvarExpression : VariableBase, IAstNode
    {
        public string LVarName { get; set; }

        protected override IConnection Connection => BravoFSUIPCConnection.Connection;

        public override string Identifier => $"L:{LVarName}";

        public override string ToString()
        {
            return Identifier;
        }
    }
}
