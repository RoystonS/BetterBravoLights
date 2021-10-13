using System.Globalization;
using BravoLights.Common.Ast;
using sly.lexer;
using sly.parser.generator;

namespace BravoLights.Common
{
#pragma warning disable CA1822 // Mark members as static
    public abstract class ExpressionParserBase
    {
        [Production("logicalExpression: OFF")]
        [Production("logicalExpression: ON")]
        public IAstNode LiteralBool(Token<ExpressionToken> token)
        {
            return new LiteralBoolNode(token.Value == "ON");
        }

        [Production("primary: HEX_NUMBER")]
        public IAstNode NumericExpressionFromLiteralNumber(Token<ExpressionToken> offsetToken)
        {
            var text = offsetToken.Value;
            var num = text[2..];

            // TODO: error handling
            var value = int.Parse(num, NumberStyles.HexNumber);
            return new LiteralNumericNode(value);
        }

        [Production("primary: DECIMAL_NUMBER")]
        public IAstNode NumericExpressionFromDecimalNumber(Token<ExpressionToken> offsetToken)
        {
            var text = offsetToken.Value;

            // TODO: error handling
            var value = double.Parse(text, CultureInfo.InvariantCulture);
            return new LiteralNumericNode(value);
        }

        [Production("numericExpression: term PLUS numericExpression")]
        [Production("numericExpression: term MINUS numericExpression")]
        [Production("term: factor TIMES term")]
        [Production("term: factor DIVIDE term")]
        public IAstNode NumberExpression(IAstNode lhs, Token<ExpressionToken> token, IAstNode rhs)
        {
            return BinaryNumericExpression.Create(lhs, token, rhs);
        }

        [Production("numericExpression: term")]
        [Production("term: factor")]
        [Production("factor: primary")]
        [Production("logicalPrimary: comparison")]
        [Production("logicalTerm: logicalPrimary")]
        [Production("logicalExpression: logicalTerm")]
        public IAstNode Direct(IAstNode node)

        {
            return node;
        }

        [Production("logicalExpression: logicalTerm OR logicalExpression")]
        [Production("logicalTerm: logicalPrimary AND logicalTerm")]
        public IAstNode LogicalJunction(IAstNode lhs, Token<ExpressionToken> token, IAstNode rhs)
        {
            return BooleanLogicalExpression.Create(lhs, token, rhs);
        }

        [Production("comparison: numericExpression COMPARISON numericExpression")]
        public IAstNode Comparison(IAstNode lhs, Token<ExpressionToken> token, IAstNode rhs)
        {
            return ComparisonExpression.Create(lhs, token, rhs);
        }


        [Production("primary: LPAREN [d] numericExpression RPAREN [d]")]
        [Production("logicalPrimary: LPAREN [d] logicalExpression RPAREN [d]")]
        public IAstNode Parens(IAstNode exp)
        {
            return exp;
        }

    }
    #pragma warning restore CA1822 // Mark members as static
}

