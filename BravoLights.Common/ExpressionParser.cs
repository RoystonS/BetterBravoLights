using System;
using System.Globalization;
using BravoLights.Common.Ast;
using sly.lexer;
using sly.parser;
using sly.parser.generator;

namespace BravoLights.Common
{
    #pragma warning disable CA1822 // Mark members as static
    public abstract class ExpressionParserBase
    {
        [Production("logical_literal: OFF")]
        [Production("logical_literal: ON")]
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

        [Production("expr_6_LOGICAL_OR: expr_7_LOGICAL_AND LOGICAL_OR expr_6_LOGICAL_OR")]
        [Production("expr_7_LOGICAL_AND: expr_8_BITWISE_OR LOGICAL_AND expr_7_LOGICAL_AND")]
        public IAstNode LogicalJunction(IAstNode lhs, Token<ExpressionToken> token, IAstNode rhs)
        {
            return BooleanLogicalExpression.Create(lhs, token, rhs);
        }

        [Production("expr_8_BITWISE_OR: expr_10_BITWISE_AND BITWISE_OR expr_8_BITWISE_OR")]
        [Production("expr_10_BITWISE_AND: expr_11_NOT BITWISE_AND expr_10_BITWISE_AND")]
        [Production("expr_14_PLUS_MINUS: expr_15_TIMES_DIVIDE [ PLUS | MINUS ] expr_14_PLUS_MINUS")]
        [Production("expr_15_TIMES_DIVIDE: expr_17_MINUS [ TIMES | DIVIDE ] expr_15_TIMES_DIVIDE")]
        public IAstNode NumberExpression(IAstNode lhs, Token<ExpressionToken> token, IAstNode rhs)
        {
            return BinaryNumericExpression.Create(lhs, token, rhs);
        }

        [Production("expr_12_COMPARISON: expr_14_PLUS_MINUS COMPARISON expr_14_PLUS_MINUS")]
        public IAstNode Comparison(IAstNode lhs, Token<ExpressionToken> token, IAstNode rhs)
        {
            return ComparisonExpression.Create(lhs, token, rhs);
        }

        [Production("expr_6_LOGICAL_OR: expr_7_LOGICAL_AND")]
        [Production("expr_7_LOGICAL_AND: expr_8_BITWISE_OR")]
        [Production("expr_8_BITWISE_OR: expr_10_BITWISE_AND")]
        [Production("expr_10_BITWISE_AND: expr_11_NOT")]
        [Production("expr_11_NOT: expr_12_COMPARISON")]
        [Production("expr_12_COMPARISON: expr_14_PLUS_MINUS")]
        [Production("expr_14_PLUS_MINUS: expr_15_TIMES_DIVIDE")]
        [Production("expr_15_TIMES_DIVIDE: expr_17_MINUS")]
        [Production("expr_17_MINUS: MSFSExpressionParser_operand")]

        [Production("MSFSExpressionParser_operand: numeric_literal")]
        [Production("MSFSExpressionParser_operand: group")]
        [Production("MSFSExpressionParser_operand: logical_literal")]
        [Production("MSFSExpressionParser_operand: primary")]

        [Production("MSFSExpressionParser_expressions: expr_6_LOGICAL_OR")]
        public IAstNode Direct(IAstNode node)

        {
            return node;
        }

        [Production("expr_17_MINUS: MINUS [d] MSFSExpressionParser_operand")]
        public IAstNode UnaryMinus(IAstNode child)
        {
            return new UnaryMinusExpression(child);
        }

        [Production("expr_11_NOT: NOT [d] expr_12_COMPARISON")]
        public IAstNode LogicalUnary(IAstNode child)
        {
            return new NotExpression(child);
        }

        [Production("group: LPAREN [d] MSFSExpressionParser_expressions RPAREN [d]")]
        public IAstNode Parens(IAstNode exp)
        {
            return exp;
        }

        private static Parser<ExpressionToken, IAstNode> cachedParser;

        public static IAstNode Parse<T>(string expression) where T : ExpressionParserBase, new()
        {
            if (cachedParser == null)
            {
                var parserInstance = new T();
                var builder = new ParserBuilder<ExpressionToken, IAstNode>();
                var parser = builder.BuildParser(parserInstance, ParserType.EBNF_LL_RECURSIVE_DESCENT, "MSFSExpressionParser_expressions");
                if (parser.IsError)
                {
                    throw new Exception($"Could not create parser. BNF is not valid. {parser.Errors[0].Message}");
                }
                cachedParser = parser.Result;
            }

            // To simplify an ambiguous lexer which would result from having both && and & as well as || and |, we'll
            // simplify the incoming expression by turning && into AND and || into OR:
            expression = expression.Replace("&&", " AND ");
            expression = expression.Replace("||", " OR ");

            var parseResult = cachedParser.Parse(expression);
            if (parseResult.IsError)
            {
                return new ErrorNode
                {
                    ErrorText = parseResult.Errors[0].ErrorMessage
                };
            }

            return parseResult.Result;
        }
    }
    #pragma warning restore CA1822 // Mark members as static
}

