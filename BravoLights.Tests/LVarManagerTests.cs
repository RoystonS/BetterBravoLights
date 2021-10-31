using System;
using System.Collections.Generic;
using BravoLights.Ast;
using BravoLights.Common;
using BravoLights.Connections;
using Moq;
using Xunit;

namespace BravoLights.Tests
{
    public class LVarManagerTests
    {
        class LVarData : ILVarData
        {
            public short ValueCount { get; set; }

            public short[] Ids { get; set; }

            public double[] Values { get; set; }
        }

        class TestListener
        {
            public object LastValue = null;

            public void Listener(object sender, ValueChangedEventArgs e)
            {
                LastValue = e.NewValue;
                if (LastValue is Exception)
                {
                    LastValue = ((Exception)LastValue).Message;
                }
            }
        }

        [Fact]
        public void AllowSubscriptionBeforeLVarIsKnown()
        {
            var mgr = new LVarManager();
            var mockWasmChannel = new Mock<IWASMChannel>(MockBehavior.Strict);
            mockWasmChannel.SetupGet(c => c.SimState).Returns(SimState.SimRunning);

            mgr.SetWASMChannel(mockWasmChannel.Object);

            var expression = new LvarExpression { LVarName = "Var2" };

            var testListener = new TestListener();
            mgr.AddListener(expression, testListener.Listener);

            Assert.Equal("LVar does not exist yet; this aircraft may not support it", testListener.LastValue);

            mockWasmChannel.Setup(c => c.ClearSubscriptions());
            mockWasmChannel.Setup(c => c.Subscribe(1)); // 0-indexed, so Var2 is 1
            // After this point if the LVar does appear, we should subscribe to it.
            mgr.UpdateLVarList(new List<string>() { "Var1", "Var2", "Var3" });
            mockWasmChannel.VerifyAll();

            // The LVar is now known but we've not had an actual valuep
            Assert.Equal("No value yet received from simulator", testListener.LastValue);
        }

        [Fact]
        public void ReportsLackOfValueForNewlySubscribedLVar()
        {
            var mgr = new LVarManager();
            var mockWasmChannel = new Mock<IWASMChannel>();
            mockWasmChannel.SetupGet(c => c.SimState).Returns(SimState.SimRunning);

            mgr.SetWASMChannel(mockWasmChannel.Object);
            mgr.UpdateLVarList(new List<string>() { "Var1", "Var2", "Var3" });

            var expression = new LvarExpression { LVarName = "Var2" };

            var testListener = new TestListener();
            mgr.AddListener(expression, testListener.Listener);

            Assert.Equal("No value yet received from simulator", testListener.LastValue);
        }

        [Fact]
        public void ReportsValueForExistingSubscribedLVar()
        {
            var mgr = new LVarManager();
            var mockWasmChannel = new Mock<IWASMChannel>();
            mockWasmChannel.SetupGet(c => c.SimState).Returns(SimState.SimRunning);

            mgr.SetWASMChannel(mockWasmChannel.Object);
            mgr.UpdateLVarList(new List<string>() { "Var1", "Var2", "Var3" });

            var expression = new LvarExpression { LVarName = "Var2" };

            // Add existing subscription and deliver a value for it
            var mockListener1 = new Mock<EventHandler<ValueChangedEventArgs>>();
            mgr.AddListener(expression, mockListener1.Object);
            var data = new LVarData { ValueCount = 1, Ids = new short[] { 1 }, Values = new double[] { 42 } };
            mgr.UpdateLVarValues(data);

            // Make a new subscription; it should be given the latest value immediately
            var testListener = new TestListener();
            mgr.AddListener(expression, testListener.Listener);
            Assert.Equal(42.0, testListener.LastValue);
        }

        [Fact]
        public void UnsubscribesWhenLastListenerRemovedForAnExpression()
        {
            var mgr = new LVarManager();
            var mockWasmChannel = new Mock<IWASMChannel>();
            mockWasmChannel.SetupGet(c => c.SimState).Returns(SimState.SimRunning);

            mgr.SetWASMChannel(mockWasmChannel.Object);
            mgr.UpdateLVarList(new List<string>() { "Var1", "Var2", "Var3" });

            var expression = new LvarExpression { LVarName = "Var2" };
            var mockListener1 = new Mock<EventHandler<ValueChangedEventArgs>>();
            var mockListener2 = new Mock<EventHandler<ValueChangedEventArgs>>();
            mgr.AddListener(expression, mockListener1.Object);
            mgr.AddListener(expression, mockListener2.Object);

            mockWasmChannel.Verify(c => c.Unsubscribe(It.IsAny<short>()), Times.Never);
            mgr.RemoveListener(expression, mockListener1.Object);
            mockWasmChannel.Verify(c => c.Unsubscribe(It.IsAny<short>()), Times.Never);
            mgr.RemoveListener(expression, mockListener2.Object);
            mockWasmChannel.Verify(c => c.Unsubscribe(1));


            // Check that we've safely unsubscribed (removed the handler)
            mockWasmChannel.SetupGet(c => c.SimState).Returns(SimState.SimExited);
            mockWasmChannel.Raise(c => c.OnSimStateChanged += null, new SimStateEventArgs { SimState = SimState.SimExited });
        }

        [Fact]
        public void NotifiesThatSimIsDisconnectedWhenTheSimExits()
        {
            var mgr = new LVarManager();
            var mockWasmChannel = new Mock<IWASMChannel>();
            mockWasmChannel.SetupGet(c => c.SimState).Returns(SimState.SimRunning);

            mgr.SetWASMChannel(mockWasmChannel.Object);
            mgr.UpdateLVarList(new List<string>() { "Var1", "Var2", "Var3" });

            var expression1 = new LvarExpression { LVarName = "Var1" };
            var expression2 = new LvarExpression { LVarName = "Var2" };

            var testListener1 = new TestListener();
            var testListener2 = new TestListener();
            mgr.AddListener(expression1, testListener1.Listener);
            mgr.AddListener(expression2, testListener2.Listener);

            Assert.Equal("No value yet received from simulator", testListener1.LastValue);
            Assert.Equal("No value yet received from simulator", testListener2.LastValue);

            mockWasmChannel.SetupGet(c => c.SimState).Returns(SimState.SimExited);
            mockWasmChannel.Raise(c => c.OnSimStateChanged += null, new SimStateEventArgs { SimState = SimState.SimExited });

            Assert.Equal("No connection to simulator", testListener1.LastValue);
            Assert.Equal("No connection to simulator", testListener2.LastValue);

            mockWasmChannel.SetupGet(c => c.SimState).Returns(SimState.SimRunning);
            mockWasmChannel.Raise(c => c.OnSimStateChanged += null, new SimStateEventArgs { SimState = SimState.SimRunning });
            mgr.UpdateLVarList(new List<string>() { "Var2" });

            // Newly-started sim doesn't have this variable at all
            Assert.Equal("LVar does not exist yet; this aircraft may not support it", testListener1.LastValue);
            // Newly-started sim does have this variable but we haven't had a value yet
            Assert.Equal("No value yet received from simulator", testListener2.LastValue);
        }
    }
}
