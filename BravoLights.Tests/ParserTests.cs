using System.Globalization;
using System.Threading;
using BravoLights.Ast;
using BravoLights.Common;
using BravoLights.Common.Ast;
using Xunit;

namespace BravoLights.Tests
{
    public class ParserTests
    {
        [Fact]
        public void ParserUsesCorrectPrecedenceForArithmeticOperators()
        {
            var parsed = MSFSExpressionParser.Parse("1 + 2 * 3 + 4 == 11");
            Assert.Null(parsed.ErrorText);

            var valueReported = false;

            parsed.ValueChanged += delegate (object sender, ValueChangedEventArgs e)
            {
                valueReported = true;
                Assert.Equal(true, e.NewValue);
            };
            Assert.True(valueReported);
        }

        [Theory]
        [InlineData("1 + 2 * 3", 7.0)]
        [InlineData("2 - 3 / 4", 1.25)]
        [InlineData("-3 - -4", 1.0)]
        [InlineData("-3--4", 1.0)]
        [InlineData("-3+-4", -7.0)]
        [InlineData("-(1+2)", -3.0)]
        [InlineData("-(1+2 * 3)", -7.0)]
        [InlineData("3 * -2", -6.0)]
        [InlineData("9 & 8", 8.0)]
        [InlineData("7 & 8", 0.0)]
        [InlineData("8 & 8", 8.0)]
        [InlineData("1 + 7 & 15 - 7", 8.0)] // & binds lower than + and -
        [InlineData("1 | 2", 3.0)]
        [InlineData("3 | 5", 7.0)]
        [InlineData("1 + 3 | 3 - 1", 6.0)] // | binds lower than + and -
        public void LiteralNumericExpressionsEvaluateCorrectly(string expression, object value)
        {
            var numericExpression = ParseAsNumeric(expression);

            ValueChangedEventArgs receivedEventArgs = null;

            numericExpression.ValueChanged += delegate (object sender, ValueChangedEventArgs e)
            {
                receivedEventArgs = e;
            };

            Assert.NotNull(receivedEventArgs);
            Assert.Equal(value, receivedEventArgs.NewValue);
        }

        [Fact]
        public void ParserUsesUsEnglishLocaleForParsingNumbers()
        {
            var thread = Thread.CurrentThread;
            var originalCulture = thread.CurrentCulture;

            try
            {
                // Switch into Italian and check that parsing still works correctly
                thread.CurrentCulture = new CultureInfo("it-IT");

                var parsed = MSFSExpressionParser.Parse("1 < 2.5 && 3.0 < 4");
                Assert.Null(parsed.ErrorText);
                Assert.Equal("((1 < 2.5) AND (3 < 4))", parsed.ToString());
            }
            finally
            {
                thread.CurrentCulture = originalCulture;
            }
        }

        [Theory]
        [InlineData("A:MULTI WORD SIMVAR, bool", "A:MULTI WORD SIMVAR, bool")]
        [InlineData("A:MULTI WORD SIMVAR, pounds per square inch", "A:MULTI WORD SIMVAR, pounds per square inch")]
        [InlineData("A:MULTI WORD SIMVAR, pounds per square inch + 1", "(A:MULTI WORD SIMVAR, pounds per square inch + 1)")]
        [InlineData("L:XMLVAR_Something", "L:XMLVAR_Something")]
        [InlineData("L:LVAR WITH SPACES IN THE NAME < 50", "(L:LVAR WITH SPACES IN THE NAME < 50)")] // https://github.com/RoystonS/BetterBravoLights/issues/47
        [InlineData("L:XMLVAR_Something + 1", "(L:XMLVAR_Something + 1)")]
        [InlineData("L:B748_Engine_AntiIce_Switch_State:1 == 1", "(L:B748_Engine_AntiIce_Switch_State:1 == 1)")] // https://github.com/RoystonS/BetterBravoLights/issues/76
        [InlineData("1 + 2", "(1 + 2)")]
        [InlineData("1 + 2 - 3", "((1 + 2) - 3)")]
        [InlineData("1 + 2 & 3 + 4", "((1 + 2) & (3 + 4))")]
        [InlineData("1 + (2 - 3)", "(1 + (2 - 3))")]
        [InlineData("1 + 2 - 3 + 4", "(((1 + 2) - 3) + 4)")]
        [InlineData("1 + 2 * 3", "(1 + (2 * 3))")]
        [InlineData("(1 + 2) * 3", "((1 + 2) * 3)")]
        [InlineData("1 + 2 * 3 + 4", "((1 + (2 * 3)) + 4)")]
        [InlineData("-1", "-1")]
        [InlineData("-1 + 2", "(-1 + 2)")]
        [InlineData("2 + -1", "(2 + -1)")]
        [InlineData("2 + -(1 + 3)", "(2 + -(1 + 3))")]
        [InlineData("2 + A:FOO,bool", "(2 + A:FOO, bool)")]
        [InlineData("2 + A:FOO BAR,bool", "(2 + A:FOO BAR, bool)")]
        [InlineData("1 * 2 + 3", "((1 * 2) + 3)")]
        [InlineData("1 * (2 + 3)", "(1 * (2 + 3))")]
        public void NumericParser(string expression, string expected)
        {
            CheckParserValue(expected, expression, ParseAsNumeric);
        }

        [Theory]
        [InlineData("ON", "ON", "ON")]
        [InlineData("ON AND OFF", "(ON AND OFF)", "OFF")]
        [InlineData("ON OR OFF", "(ON OR OFF)", "ON")]
        [InlineData("OFF", "OFF", "OFF")]
        [InlineData("A:FOO, bool < 42", "(A:FOO, bool < 42)", "(A:FOO, bool < 42)")]
        [InlineData("L:BAR == 1", "(L:BAR == 1)", "(L:BAR == 1)")]
        [InlineData("3 < 4", "(3 < 4)", "(3 < 4)")]
        [InlineData("(1 + 3) < (3 + 4)", "((1 + 3) < (3 + 4))", "((1 + 3) < (3 + 4))")]
        [InlineData("3 < 4 OR 4 < 5", "((3 < 4) OR (4 < 5))", "((3 < 4) OR (4 < 5))")]
        [InlineData("(2 + 3) < (A:FOO,bool + 12)", "((2 + 3) < (A:FOO, bool + 12))", "((2 + 3) < (A:FOO, bool + 12))")]
        [InlineData("(2 + 3) <= (A:FOO,bool + 12)", "((2 + 3) <= (A:FOO, bool + 12))", "((2 + 3) <= (A:FOO, bool + 12))")]
        [InlineData("(2 + 3) >= (A:FOO,bool + 12)", "((2 + 3) >= (A:FOO, bool + 12))", "((2 + 3) >= (A:FOO, bool + 12))")]
        [InlineData("(2 + 3) > (A:FOO,bool + 12)", "((2 + 3) > (A:FOO, bool + 12))", "((2 + 3) > (A:FOO, bool + 12))")]
        [InlineData("3 == 4", "(3 == 4)", "(3 == 4)")]
        [InlineData("3 != 4", "(3 != 4)", "(3 != 4)")]
        [InlineData("1 + 2 < 3 + 4", "((1 + 2) < (3 + 4))", "((1 + 2) < (3 + 4))")]
        [InlineData("1 + 2 * 3 < 3 * 4 - 5", "((1 + (2 * 3)) < ((3 * 4) - 5))", "((1 + (2 * 3)) < ((3 * 4) - 5))")]
        [InlineData("1<2 AND 2>3", "((1 < 2) AND (2 > 3))", "((1 < 2) AND (2 > 3))")]

        // Comparison operators
        [InlineData("1 < 2 && 1 <= 2 && 1 == 2 && 1 >= 2 && 1 > 2 && 1 != 2 && 1 <> 2", "(((((((1 < 2) AND (1 <= 2)) AND (1 == 2)) AND (1 >= 2)) AND (1 > 2)) AND (1 != 2)) AND (1 != 2))",
            "(((((((1 < 2) AND (1 <= 2)) AND (1 == 2)) AND (1 >= 2)) AND (1 > 2)) AND (1 != 2)) AND (1 != 2))")]

        // AND/OR precedence. (AND binds tighter)
        [InlineData("ON AND OFF OR ON AND OFF", "((ON AND OFF) OR (ON AND OFF))", "OFF")]
        [InlineData("ON AND (OFF OR ON) AND OFF", "((ON AND (OFF OR ON)) AND OFF)", "OFF")]
        [InlineData("1==1 || 2==2 && 3==3 || 4==4", "(((1 == 1) OR ((2 == 2) AND (3 == 3))) OR (4 == 4))", "(((1 == 1) OR ((2 == 2) AND (3 == 3))) OR (4 == 4))")]

        // Alternate AND/OR
        [InlineData("1==1 AND 2==2 && 3==3 OR 4==4 || 5==5", "(((((1 == 1) AND (2 == 2)) AND (3 == 3)) OR (4 == 4)) OR (5 == 5))", "(((((1 == 1) AND (2 == 2)) AND (3 == 3)) OR (4 == 4)) OR (5 == 5))")]

        // NOT checks. (NOT binds tightly to logicals and loosely to numerics
        [InlineData("NOT ON", "(NOT ON)", "OFF")]
        [InlineData("NOT ON AND OFF", "((NOT ON) AND OFF)", "OFF")]
        [InlineData("NOT ON OR OFF", "((NOT ON) OR OFF)", "OFF")]
        [InlineData("NOT 1 == 2", "(NOT (1 == 2))", "(NOT (1 == 2))")]
        [InlineData("NOT ON AND NOT OFF OR NOT ON OR NOT OFF", "((((NOT ON) AND (NOT OFF)) OR (NOT ON)) OR (NOT OFF))", "ON")]

        // Unary minus checks
        [InlineData("- A:FOO, bool > 3", "(-A:FOO, bool > 3)", "(-A:FOO, bool > 3)")]
        [InlineData("-(A:FOO, bool) > 3", "(-A:FOO, bool > 3)", "(-A:FOO, bool > 3)")]

        // Optimized cases
        [InlineData("(A:FOO, bool > 3) AND (A:BAR, bool > 1 AND (ON OR OFF))",
            "((A:FOO, bool > 3) AND ((A:BAR, bool > 1) AND (ON OR OFF)))",
            "((A:FOO, bool > 3) AND (A:BAR, bool > 1))")]
        [InlineData("(A:FOO, bool > 3) OR (A:BAR, bool > 1 OR (ON AND OFF))",
            "((A:FOO, bool > 3) OR ((A:BAR, bool > 1) OR (ON AND OFF)))",
            "((A:FOO, bool > 3) OR (A:BAR, bool > 1))")]
        public void BooleanParser(string expression, string expected, string expectedOptimized)
        {
            CheckParserValue(expected, expression, ParseAsBoolean);

            Assert.Equal(expectedOptimized, ParseAsBoolean(expression).Optimize().ToString());
        }

        [Theory]
        [InlineData("(2 + 3", "unexpected end of stream. Expecting <<EOS>>.")]
        [InlineData("A:SOME VARIABLE == 3", "Missing units for variable 'A:SOME VARIABLE'.")]
        public void ParsingErrors(string expression, string expectedError)
        {
            var parseResult = MSFSExpressionParser.Parse(expression);
            Assert.NotNull(parseResult.ErrorText);
            Assert.Equal(expectedError, parseResult.ErrorText);
        }

        private delegate IAstNode ParseFunction(string expression);

        private static void CheckParserValue(string expected, string expression, ParseFunction parser)
        {
            // The expression should re-parse to the expected value.
            Assert.Equal(expected, parser(expression).ToString());
            // The expected value should parse to itself
            Assert.Equal(expected, parser(expected).ToString());
        }

        private static IAstNode ParseAsNumeric(string expression)
        {
            // The expression parser only parses boolean expressions so we make a boolean expression
            // out of the incoming numeric expression, and then pull it apart
            var booleanExpression = $"({expression}) > 0";
            var parsed = MSFSExpressionParser.Parse(booleanExpression);
            Assert.Null(parsed.ErrorText);
            var binaryExpression = parsed as GtComparison;
            var originalExpression = binaryExpression.Lhs;
            return originalExpression;
        }

        private static IAstNode ParseAsBoolean(string expression)
        {
            return MSFSExpressionParser.Parse(expression);
        }
    }
}
