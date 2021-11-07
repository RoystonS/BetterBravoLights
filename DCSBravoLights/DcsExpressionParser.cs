using BravoLights.Common;
using BravoLights.Common.Ast;
using sly.lexer;
using sly.parser.generator;

namespace DCSBravoLights
{
    class DcsExpressionParser : ExpressionParserBase
    {        
        [Production("primary: DCS_VAR")]
        public IAstNode DcsVarExpression(Token<ExpressionToken> token)
        {
            // [Category:Identifier]
            var text = token.Value[1..^1].Trim();
            var bits = text.Split(":");
            var category = bits[0];
            var identifier = bits[1];
            return new DcsVariableExpression(category, identifier);
        }

        [Operand]
        [Production("group : LPAREN DcsExpressionParser_expressions RPAREN")]
        public IAstNode Group(Token<ExpressionToken> lparen, IAstNode child, Token<ExpressionToken> rparen)
        {
            return child;
        }

        public static IAstNode Parse(string expression)
        {
            return Parse<DcsExpressionParser>(expression);
        }
    }
}
