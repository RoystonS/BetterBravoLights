using System;
using BravoLights.Ast;
using BravoLights.Common;
using BravoLights.Connections;
using Moq;
using Xunit;

namespace BravoLights.Tests
{
    public class ExpressionCombinationTests
    {
        private void SetupLVarManager()
        {
            var mockWasmChannel = new Mock<IWASMChannel>();
            mockWasmChannel.SetupGet(c => c.SimState).Returns(SimState.SimRunning);
            LVarManager.Connection.SetWASMChannel(mockWasmChannel.Object);
        }

        [Fact]
        public void ORExpressionsShortcircuitCorrectly()
        {
            SetupLVarManager();

            var parse = MSFSExpressionParser.Parse("L:Var1 > 3 OR 1 == 1");

            object lastValue = null;
            parse.ValueChanged += (object sender, ValueChangedEventArgs e) =>
            {
                lastValue = e.NewValue;
            };

            // Even though the LVar does not exist we should still be able to short-circuit the result because one side of the OR is true.
            Assert.Equal(true, lastValue);
        }

        [Fact]
        public void ORExpressionsDoNotShortcircuitIfNeitherSideIsTrue()
        {
            SetupLVarManager();

            var parse = MSFSExpressionParser.Parse("L:Var1 > 3 OR 1 == 0");

            object lastValue = null;
            parse.ValueChanged += (object sender, ValueChangedEventArgs e) =>
            {
                lastValue = e.NewValue;
            };

            // One side of the OR is false, so we're dependent upon the other side, which is erroring. So the OR should error.
            Assert.IsType<Exception>(lastValue);
        }

        [Fact]
        public void ANDExpressionsShortcircuitCorrectly()
        {
            SetupLVarManager();

            var parse = MSFSExpressionParser.Parse("L:Var1 > 3 AND 1 == 0");

            object lastValue = null;
            parse.ValueChanged += (object sender, ValueChangedEventArgs e) =>
            {
                lastValue = e.NewValue;
            };

            // Even though the LVar does not exist we should still be able to short-circuit the result because one side of the AND is false
            Assert.Equal(false, lastValue);
        }

        [Fact]
        public void ANDExpressionsDoNotShortcircuitIfEitherSideIsTrue()
        {
            SetupLVarManager();

            var parse = MSFSExpressionParser.Parse("L:Var1 > 3 AND 1 == 1");

            object lastValue = null;
            parse.ValueChanged += (object sender, ValueChangedEventArgs e) =>
            {
                lastValue = e.NewValue;
            };

            // One side of the AND is true, so we're dependent upon the other side, which is erroring. So the AND should error.
            Assert.IsType<Exception>(lastValue);
        }


    }
}
