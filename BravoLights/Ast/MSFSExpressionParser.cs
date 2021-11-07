using BravoLights.Common;
using BravoLights.Common.Ast;
using Superpower;
using Superpower.Model;
using Superpower.Parsers;

namespace BravoLights.Ast
{
    public class MSFSExpressionParser : ExpressionParserBase
    {
        private static TextParser<IAstNode> CreateSimVarNode { get; } = input =>
        {
            // This parser will be given strings of the format 'A:FOO BAR' as well as 'A:FOO BAR, some units'.
            // This gives us the opportunity to complain nicely about the missing units.

            // Skip over 'A:'
            var fullName = input.Skip(2).ToString().Trim();
            var remainder = input.Skip(input.Length);

            var bits = fullName.Split(",", 2);
            if (bits.Length == 1)
            {
                // No ", units" segment
                throw new ParseException($"Missing units for variable 'A:{fullName}'.", input.Position);
            }

            var name = bits[0].Trim();
            var units = bits[1].Trim();

            IAstNode node = new SimVarExpression(name, units);
            return Result.Value(node, input, remainder);
        };

        private static TextParser<IAstNode> CreateLVarNode { get; } = input =>
        {
            // Skip over 'L:'
            var fullName = input.Skip(2).ToString();
            var remainder = input.Skip(2 + fullName.Length);

            IAstNode node = new LvarExpression { LVarName = fullName.Trim() };
            return Result.Value(node, input, remainder);
        };

        protected override TokenListParser<ExpressionToken, IAstNode> VariableParser
        {
            get
            {
                var simvar = Token.EqualTo(ExpressionToken.SIMVAR).Apply(CreateSimVarNode);
                var lvar = Token.EqualTo(ExpressionToken.LVAR).Apply(CreateLVarNode);

                return simvar.Or(lvar);
            }
        }

        public static IAstNode Parse(string expression)
        {
            var parser = new MSFSExpressionParser();
            return parser.ParseExpression<MSFSExpressionParser>(expression);
        }
    }
}
