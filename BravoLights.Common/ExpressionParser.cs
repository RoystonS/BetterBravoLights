using System.Text.RegularExpressions;
using BravoLights.Common.Ast;
using Superpower;
using Superpower.Model;
using Superpower.Parsers;
using Superpower.Tokenizers;

namespace BravoLights.Common
{
    public abstract class ExpressionParserBase
    {
        private readonly TextParser<TextSpan> SimulationVariable = Span.Regex("A:[:A-Z_a-z0-9 ]+(,\\s*[A-Z_a-z0-9 ]+)?", RegexOptions.Compiled);
        private readonly TextParser<TextSpan> LVar = Span.Regex("L:[:A-Z_a-z0-9 ]+", RegexOptions.Compiled);
        private readonly TextParser<TextSpan> Double = Span.Regex("-?[0-9]+(\\.[0-9]+)?", RegexOptions.Compiled);
        private readonly TextParser<TextSpan> HexInteger = Span.Regex("0x[0-9A-F]+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        protected virtual TokenizerBuilder<ExpressionToken> ConfigureTokenizerBuilder(TokenizerBuilder<ExpressionToken> builder)
        {
            return builder
                .Match(SimulationVariable, ExpressionToken.SIMVAR)
                .Match(LVar, ExpressionToken.LVAR);
        }

        public Tokenizer<ExpressionToken> CreateTokenizer()
        {
            var builder = new TokenizerBuilder<ExpressionToken>();
            builder = ConfigureTokenizerBuilder(builder);
            return builder
                .Match(HexInteger, ExpressionToken.HEX_NUMBER)
                .Match(Double, ExpressionToken.DECIMAL_NUMBER)
                .Match(Span.EqualTo("ON"), ExpressionToken.ON)
                .Match(Span.EqualTo("OFF"), ExpressionToken.OFF)
                .Match(Span.EqualTo("AND"), ExpressionToken.LOGICAL_AND)
                .Match(Span.EqualTo("OR"), ExpressionToken.LOGICAL_OR)
                .Match(Span.EqualTo("NOT"), ExpressionToken.NOT)
                .Match(Character.EqualTo('+'), ExpressionToken.PLUS)
                .Match(Character.EqualTo('-'), ExpressionToken.MINUS)
                .Match(Character.EqualTo('*'), ExpressionToken.TIMES)
                .Match(Character.EqualTo('/'), ExpressionToken.DIVIDE)
                .Match(Character.EqualTo('&'), ExpressionToken.BITWISE_AND)
                .Match(Character.EqualTo('|'), ExpressionToken.BITWISE_OR)
                .Match(Character.EqualTo('('), ExpressionToken.LPAREN)
                .Match(Character.EqualTo(')'), ExpressionToken.RPAREN)
                .Match(Span.EqualTo("<="), ExpressionToken.COMPARISON)
                .Match(Span.EqualTo("<>"), ExpressionToken.COMPARISON)
                .Match(Span.EqualTo("<"), ExpressionToken.COMPARISON)
                .Match(Span.EqualTo(">="), ExpressionToken.COMPARISON)
                .Match(Span.EqualTo(">"), ExpressionToken.COMPARISON)
                .Match(Span.EqualTo("=="), ExpressionToken.COMPARISON)
                .Match(Span.EqualTo("!="), ExpressionToken.COMPARISON)
                .Ignore(Span.WhiteSpace)
                .Build();
        }

        private static TextParser<string> MakeComparison { get; } = input =>
        {
            var fullOp = input.ToString();
            return Result.Value(fullOp, input, input.Skip(fullOp.Length));
        };

        public void Initialize()
        {
            var add = Token.EqualTo(ExpressionToken.PLUS);
            var subtract = Token.EqualTo(ExpressionToken.MINUS);
            var times = Token.EqualTo(ExpressionToken.TIMES);
            var divide = Token.EqualTo(ExpressionToken.DIVIDE);
            var bitwiseAND = Token.EqualTo(ExpressionToken.BITWISE_AND);
            var bitwiseOR = Token.EqualTo(ExpressionToken.BITWISE_OR);
            var logicalAND = Token.EqualTo(ExpressionToken.LOGICAL_AND);
            var logicalOR = Token.EqualTo(ExpressionToken.LOGICAL_OR);
            var numericComparison = Token.EqualTo(ExpressionToken.COMPARISON).Apply(MakeComparison);

            var numericOperand = HexNumberParser.Or(DecimalNumberParser).Or(this.VariableParser);

            var literalBool = Token.EqualTo(ExpressionToken.ON).Value(LiteralBoolNode.On).Or(Token.EqualTo(ExpressionToken.OFF).Value(LiteralBoolNode.Off));

            var factorParser =
                (from lparen in Token.EqualTo(ExpressionToken.LPAREN)
                 from expr in Parse.Ref(() => BooleanRootParser)
                 from rparen in Token.EqualTo(ExpressionToken.RPAREN)
                 select expr)
                .Or(numericOperand)
                .Or(literalBool);

            var unaryMinus =
                from sign in Token.EqualTo(ExpressionToken.MINUS)
                from factor in factorParser
                select UnaryMinusExpression.Create(factor);

            var precedence17 = unaryMinus.Or(factorParser).Named("expression");

            var precedence15 = Parse.Chain(times.Or(divide), precedence17, BinaryNumericExpression.Create);
            var precedence14 = Parse.Chain(add.Or(subtract), precedence15, BinaryNumericExpression.Create);
            var precedence10 = Parse.Chain(bitwiseAND, precedence14, BinaryNumericExpression.Create);
            var precedence09 = Parse.Chain(bitwiseOR, precedence10, BinaryNumericExpression.Create);
            NumericRootParser = precedence09;

            var NumericComparison = Parse.Chain(numericComparison, NumericRootParser, ComparisonExpression.Create);

            var unaryNot =
                from op in Token.EqualTo(ExpressionToken.NOT)
                from factor in NumericComparison
                select NotExpression.Create(factor);

            var precedence08 = unaryNot.Or(NumericComparison);
            var precedence07 = Parse.Chain(logicalAND, precedence08, BooleanLogicalExpression.Create);
            var precedence06 = Parse.Chain(logicalOR, precedence07, BooleanLogicalExpression.Create);

            BooleanRootParser = precedence06;
        }

        private TokenListParser<ExpressionToken, IAstNode> NumericRootParser;
        private TokenListParser<ExpressionToken, IAstNode> BooleanRootParser;

        protected abstract TokenListParser<ExpressionToken, IAstNode> VariableParser { get; }

        private static readonly TextParser<double> HexParser = input =>
        {
            var num = long.Parse(input.ToStringValue()[2..], System.Globalization.NumberStyles.HexNumber);

            return Result.Value((double)num, input, input.Skip(input.Length));
        };

        private readonly TokenListParser<ExpressionToken, IAstNode> DecimalNumberParser = Token.EqualTo(ExpressionToken.DECIMAL_NUMBER)
            .Apply(Numerics.DecimalDouble)
            .Select(LiteralNumericNode.Create);

        private readonly TokenListParser<ExpressionToken, IAstNode> HexNumberParser = Token.EqualTo(ExpressionToken.HEX_NUMBER)
            .Apply(HexParser)
            .Select(LiteralNumericNode.Create);

        private Tokenizer<ExpressionToken> cachedTokenizer;
        public Tokenizer<ExpressionToken> GetTokenizer()
        {
            if (cachedTokenizer == null)
            {
                cachedTokenizer = CreateTokenizer();
            }
            return cachedTokenizer;
        }

        public TokenListParser<ExpressionToken, IAstNode> GetCachedNumericRootParser()
        {
            if (NumericRootParser == null)
            {
                Initialize();
            }
            return NumericRootParser;
        }

        public TokenListParser<ExpressionToken, IAstNode> GetCachedBooleanRootParser()
        {
            if (BooleanRootParser == null)
            {
                Initialize();
            }
            return BooleanRootParser;
        }

        public IAstNode ParseWith(string expression, TokenListParser<ExpressionToken, IAstNode> parser)
        {
            // To simplify an ambiguous lexer which would result from having both && and & as well as || and |, we'll
            // simplify the incoming expression by turning && into AND and || into OR:
            expression = expression.Replace("&&", " AND ");
            expression = expression.Replace("||", " OR ");

            var tokenizer = GetTokenizer();
            var result = parser.Parse(tokenizer.Tokenize(expression));
            return result;
        }

        public IAstNode ParseExpression<T>(string expression) where T : ExpressionParserBase, new()
        {
            try
            {
                return ParseWith(expression, GetCachedBooleanRootParser());
            }
            catch (ParseException ex)
            {
                return new ErrorNode
                {
                    ErrorText = ex.Message
                };
            }
        }
    }
}

