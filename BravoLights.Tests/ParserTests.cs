using System;
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
        public void ParserCopesWithSimpleSimVarExpressions()
        {
            var parsed = MSFSExpressionParser.Parse("A:FOO, bool < 42");
            Assert.Null(parsed.ErrorText);
            Assert.Equal("(A:FOO, bool < 42)", parsed.ToString());
        }

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

        [Fact]
        public void ParserUsesCorrectPrecedenceForLogicalOperators()
        {
            var parsed = MSFSExpressionParser.Parse("1==1 || 2==2 && 3==3 || 4==4");
            Assert.Null(parsed.ErrorText);

            Assert.Equal("((1 == 1) OR (((2 == 2) AND (3 == 3)) OR (4 == 4)))", parsed.ToString());
        }

        [Fact]
        public void NotHasHigherPrecedenceThanAndAndOr()
        {
            var parsed = MSFSExpressionParser.Parse("NOT ON AND NOT OFF OR NOT ON OR NOT OFF");
            Assert.Null(parsed.ErrorText);
            Assert.Equal("(((NOT ON) AND (NOT OFF)) OR ((NOT ON) OR (NOT OFF)))", parsed.ToString());
        }

        [Fact]
        public void AllowsCStyleLogicalOperatorsAndEnglishLogicalOperators()
        {
            var parsed = MSFSExpressionParser.Parse("1==1 AND 2==2 && 3==3 OR 4==4 || 5==5");

            Assert.Null(parsed.ErrorText);

            Assert.Equal("(((1 == 1) AND ((2 == 2) AND (3 == 3))) OR ((4 == 4) OR (5 == 5)))", parsed.ToString());
        }

        [Fact]
        public void ParserSupportsNotOperator()
        {
            var parsed = MSFSExpressionParser.Parse("NOT 1==2");

            Assert.Null(parsed.ErrorText);

            Assert.Equal("(NOT (1 == 2))", parsed.ToString());
        }

        [Fact]
        public void ParserSupportsAllArithmeticOperators()
        {
            var parsed = MSFSExpressionParser.Parse("1 < 2 && 1 <= 2 && 1 == 2 && 1 >= 2 && 1 > 2 && 1 != 2");

            Assert.Null(parsed.ErrorText);

            Assert.Equal("((1 < 2) AND ((1 <= 2) AND ((1 == 2) AND ((1 >= 2) AND ((1 > 2) AND (1 != 2))))))", parsed.ToString());
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
        public void LiteralNumericExpressionsEvaluateCorrectly(string expression, object value)
        {
            // The expression parser only parses boolean expressions so we make a boolean expression
            // out of the incoming numeric expression, and then pull it apart
            var booleanExpression = $"({expression}) > 0";
            var parsed = MSFSExpressionParser.Parse(booleanExpression);
            Assert.Null(parsed.ErrorText);
            var binaryExpression = parsed as GtComparison;
            var originalExpression = binaryExpression.Lhs;

            ValueChangedEventArgs receivedEventArgs = null;

            originalExpression.ValueChanged += delegate (object sender, ValueChangedEventArgs e)
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
        [InlineData("A:FOO, bool < 42", "(A:FOO, bool < 42)")]
        [InlineData("ON", "ON")]
        [InlineData("OFF", "OFF")]
        [InlineData("ON OR OFF", "(ON OR OFF)")]
        [InlineData("- A:FOO, bool > 3", "((- A:FOO, bool) > 3)")]
        [InlineData("-(A:FOO, bool) > 3", "((- A:FOO, bool) > 3)")]
        public void ParserRoundTrips(string expression, string generated)
        {
            var parse = MSFSExpressionParser.Parse(expression);
            Assert.Null(parse.ErrorText);

            var output = parse.ToString();
            Assert.Equal(output, generated);
        }
    }
}
