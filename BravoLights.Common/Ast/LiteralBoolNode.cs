namespace BravoLights.Common.Ast
{
    /// <summary>
    /// A node which represents a literal bool.
    /// </summary>
    class LiteralBoolNode : ConstantNode<bool>
    {
        private LiteralBoolNode(bool value) : base(value)
        {
        }

        public override string ToString()
        {
            return Value ? "ON" : "OFF";
        }

        public static readonly IAstNode On = new LiteralBoolNode(true);
        public static readonly IAstNode Off = new LiteralBoolNode(false);
    }
}
