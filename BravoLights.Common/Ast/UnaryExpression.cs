﻿using System;
using System.Collections.Generic;

namespace BravoLights.Common.Ast
{
    abstract class UnaryExpression<TChildren, TOutput> : IAstNode
    {
        protected readonly IAstNode Child;

        private object lastChildValue;
        private object lastReportedValue;

        protected UnaryExpression(IAstNode child)
        {
            Child = child;
        }

        public string ErrorText => null;

        public IEnumerable<IVariable> Variables
        {
            get { return Child.Variables; }
        }

        public NodeDataType ValueType
        {
            get { return NodeDataTypeUtility.GetNodeDataType(typeof(TOutput)); }
        }

        protected abstract TOutput ComputeValue(TChildren child);

        private void HandleChildValueChanged(object sender, ValueChangedEventArgs e)
        {
            lastChildValue = e.NewValue;

            Recompute();
        }

        private void Recompute()
        {
            if (lastChildValue == null)
            {
                return;
            }

            object newValue;
            if (lastChildValue is Exception)
            {
                newValue = lastChildValue;
            }
            else
            {
                var child = (TChildren)Convert.ChangeType(lastChildValue, typeof(TChildren));
                newValue = ComputeValue(child);
            }

            if (lastReportedValue == null || !lastReportedValue.Equals(newValue)) // N.B. We must unbox before doing the comparison otherwise we'll be comparing boxed pointers
            {
                lastReportedValue = newValue;

                listeners?.Invoke(this, new ValueChangedEventArgs { NewValue = newValue });
            }
        }

        public abstract IAstNode Optimize();

        private EventHandler<ValueChangedEventArgs> listeners;

        public event EventHandler<ValueChangedEventArgs> ValueChanged
        {
            add
            {
                var subscribe = listeners == null;
                // It's important that we add the listener before subscribing to children
                // because the subscription may fire immediately
                listeners += value;
                if (subscribe)
                {
                    Child.ValueChanged += HandleChildValueChanged;
                }
                if (lastReportedValue != null)
                {
                    // New subscriber and we already have a valid computed value. Ship it.
                    value(this, new ValueChangedEventArgs { NewValue = lastReportedValue });
                }
            }
            remove
            {
                listeners -= value;
                if (listeners == null)
                {
                    Child.ValueChanged -= HandleChildValueChanged;
                }
            }
        }
    }
}
