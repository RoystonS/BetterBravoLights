using System;
using System.Globalization;
using BravoLights.Ast;
using sly.lexer;
using sly.parser;
using sly.parser.generator;

namespace BravoLights
{
    public class ExpressionParser
    {
        [Production("logicalExpression: OFF")]
        [Production("logicalExpression: ON")]
        public IAstNode LiteralBool(Token<ExpressionToken> token)
        {
            return new LiteralBoolNode(token.Value == "ON");
        }

        [Production("primary: LVAR")]
        public IAstNode Lvar(Token<ExpressionToken> token)
        {
            var text = token.Value.Substring(2);

            return new FSUIPCLvarExpression
            {
                LVarName = text
            };
        }

        [Production("primary: SIMVAR")]
        public IAstNode SimVarExpression(Token<ExpressionToken> simvarToken)
        {
            var text = simvarToken.Value.Substring(2);
            var bits = text.Split(",");
            var varName = bits[0];
            var type = bits[1].Trim();
            return new SimVarExpression(varName, type);
        }

        [Production("primary: OFFSET")]
        public IAstNode FSUIPCOffset(Token<ExpressionToken> offsetToken)
        {
            return FSUIPCOffsetExpression.Create(offsetToken.Value);
        }

        [Production("primary: HEX_NUMBER")]
        public IAstNode NumericExpressionFromLiteralNumber(Token<ExpressionToken> offsetToken)
        {
            var text = offsetToken.Value;
            var num = text.Substring(2);

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

        private static Parser<ExpressionToken, IAstNode> cachedParser;

        public static IAstNode Parse(string expression)
        {
            if (ExpressionParser.cachedParser == null)
            {
                var parserInstance = new ExpressionParser();
                var builder = new ParserBuilder<ExpressionToken, IAstNode>();
                var parser = builder.BuildParser(parserInstance, ParserType.EBNF_LL_RECURSIVE_DESCENT, "logicalExpression");
                if (parser.IsError)
                {
                    throw new Exception($"Could not create parser. BNF is not valid. {parser.Errors[0]}");
                }
                ExpressionParser.cachedParser = parser.Result;
            }

            var parseResult = ExpressionParser.cachedParser.Parse(expression);
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
}

