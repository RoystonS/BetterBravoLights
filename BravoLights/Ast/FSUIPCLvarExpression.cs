using BravoLights.Connections;

namespace BravoLights.Ast
{
    class FSUIPCLvarExpression : VariableBase, IAstNode
    {
        public string LVarName { get; set; }

        public override IConnection Connection => BravoFSUIPCConnection.Connection;

        public override string Name => $"L:{LVarName}";

        public override string ToString()
        {
            return Name;
        }
    }
}
