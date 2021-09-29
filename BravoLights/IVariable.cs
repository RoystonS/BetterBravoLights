using BravoLights.Ast;
using BravoLights.Connections;

namespace BravoLights
{
    public interface IVariable : IAstNode
    {
        string Name { get; }

        IConnection Connection { get; }
    }
}
