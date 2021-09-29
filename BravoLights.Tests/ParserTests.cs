using System;
using Xunit;

namespace BravoLights.Tests
{
    public class ParserTests
    {
        [Fact]
        public void ParserCopesWithSimpleSimVarExpressions()
        {
            var parsed = ExpressionParser.Parse("A:FOO, bool < 42");
            Assert.Null(parsed.ErrorText);
            Assert.Equal("(A:FOO, bool < 42)", parsed.ToString());
        }

        [Fact]
        public void ParserUsesCorrectPrecedenceForArithmeticOperators()
        {
            var parsed = ExpressionParser.Parse("1 + 2 * 3 + 4 == 11");
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
            var parsed = ExpressionParser.Parse("1==1 || 2==2 && 3==3 || 4==4");
            Assert.Null(parsed.ErrorText);

            Assert.Equal("((1 == 1) OR (((2 == 2) AND (3 == 3)) OR (4 == 4)))", parsed.ToString());
        }

        [Fact]
        public void AllowsCStyleLogicalOperatorsAndEnglishLogicalOperators()
        {
            var parsed = ExpressionParser.Parse("1==1 AND 2==2 && 3==3 OR 4==4 || 5==5");

            Assert.Null(parsed.ErrorText);

            Assert.Equal("(((1 == 1) AND ((2 == 2) AND (3 == 3))) OR ((4 == 4) OR (5 == 5)))", parsed.ToString());

        }

        [Fact]
        public void ParserSupportsAllArithmeticOperators()
        {
            var parsed = ExpressionParser.Parse("1 < 2 && 1 <= 2 && 1 == 2 && 1 >= 2 && 1 > 2 && 1 != 2");

            Assert.Null(parsed.ErrorText);

            Assert.Equal("((1 < 2) AND ((1 <= 2) AND ((1 == 2) AND ((1 >= 2) AND ((1 > 2) AND (1 != 2))))))", parsed.ToString());
        }

        [Theory]
        [InlineData("A:FOO, bool < 42", "(A:FOO, bool < 42)")]
        public void ParserRoundTrips(string expression, string generated)
        {
            var parse = ExpressionParser.Parse(expression);
            Assert.Null(parse.ErrorText);

            var output = parse.ToString();
            Assert.Equal(output, generated);
        }
    }
}
