using BravoLights.Common;
using BravoLights.Common.Ast;
using sly.lexer;
using sly.parser.generator;
using System;

namespace BravoLights.Ast
{
    #pragma warning disable CA1822 // Mark members as static
    public class MSFSExpressionParser : ExpressionParserBase
    {
        [Operand]
        [Production("numeric_literal: LVAR")]
        public IAstNode Lvar(Token<ExpressionToken> token)
        {
            var text = token.Value[2..];

            return new LvarExpression
            {
                LVarName = text
            };
        }

        [Operand]
        [Production("numeric_literal: SIMVAR")]
        public IAstNode SimVarExpression(Token<ExpressionToken> simvarToken)
        {
            // text will be
            // A:SIMVAR
            // or
            // A:SIMVAR, units
            // (The former isn't valid but we allow it up to this point so that we can give a nice error message about the missing units)

            var text = simvarToken.Value[2..];
            var bits = text.Split(",", 2);
            var varName = bits[0].Trim();

            if (bits.Length == 1)
            {
                throw new Exception($"Missing units for variable 'A:{varName}'.");
            }

            var type = bits[1].Trim();
            return new SimVarExpression(varName, type);
        }

        [Operand]
        [Production("group : LPAREN MSFSExpressionParser_expressions RPAREN")]
        public IAstNode Group(Token<ExpressionToken> _1, IAstNode child, Token<ExpressionToken> _2)
        {
            return child;
        }

        public static IAstNode Parse(string expression)
        {
            return Parse<MSFSExpressionParser>(expression);
        }
    }
    #pragma warning restore CA1822 // Mark members as static
}
