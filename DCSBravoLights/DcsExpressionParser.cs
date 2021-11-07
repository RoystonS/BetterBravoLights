using BravoLights.Common;
using BravoLights.Common.Ast;
using Superpower;
using Superpower.Model;
using Superpower.Parsers;

namespace DCSBravoLights
{
    class DcsExpressionParser : ExpressionParserBase
    {
        // [Category:Identifier]
        private static TextParser<IAstNode> CreateDcsVarNode { get; } = input =>
        {
            var text = input.ToStringValue()[1..^1].Trim();
            var bits = text.Split(":");
            var category = bits[0];
            var identifier = bits[1];
            IAstNode node = new DcsVariableExpression(category, identifier);

            var remainder = input.Skip(input.Length);
            return Result.Value(node, input, remainder);
        };

        protected override TokenListParser<ExpressionToken, IAstNode> VariableParser
        {
            get
            {
                var simvar = Token.EqualTo(ExpressionToken.DCS_VAR).Apply(CreateDcsVarNode);
                return simvar;
            }
        }

        public static IAstNode Parse(string expression)
        {
            var parser = new DcsExpressionParser();
            return parser.ParseExpression<DcsExpressionParser>(expression);
        }
    }
}
