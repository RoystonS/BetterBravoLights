using System;
using BravoLights.Common;
using BravoLights.Common.Ast;
using sly.lexer;
using sly.parser;
using sly.parser.generator;

namespace BravoLights.Ast
{
    #pragma warning disable CA1822 // Mark members as static
    public class MSFSExpressionParser :ExpressionParserBase
    {
        [Production("primary: LVAR")]
        public IAstNode Lvar(Token<ExpressionToken> token)
        {
            var text = token.Value[2..];

            return new FSUIPCLvarExpression
            {
                LVarName = text
            };
        }


        [Production("primary: SIMVAR")]
        public IAstNode SimVarExpression(Token<ExpressionToken> simvarToken)
        {
            var text = simvarToken.Value[2..];
            var bits = text.Split(",");
            var varName = bits[0];
            var type = bits[1].Trim();
            return new SimVarExpression(varName, type);
        }


        private static Parser<ExpressionToken, IAstNode> cachedParser;

        public static IAstNode Parse(string expression)
        {
            if (MSFSExpressionParser.cachedParser == null)
            {
                var parserInstance = new MSFSExpressionParser();
                var builder = new ParserBuilder<ExpressionToken, IAstNode>();
                var parser = builder.BuildParser(parserInstance, ParserType.EBNF_LL_RECURSIVE_DESCENT, "logicalExpression");
                if (parser.IsError)
                {
                    throw new Exception($"Could not create parser. BNF is not valid. {parser.Errors[0]}");
                }
                MSFSExpressionParser.cachedParser = parser.Result;
            }

            var parseResult = MSFSExpressionParser.cachedParser.Parse(expression);
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
