﻿using System;

namespace BravoLights.Common.Ast
{
    /// <summary>
    /// A binary expression such as 'X < Y' or 'X == Y', which produces a boolean from two other numbers and an operator.
    /// </summary>
    abstract class ComparisonExpression : BinaryExpression<bool>
    {
        protected ComparisonExpression(IAstNode lhs, IAstNode rhs) : base(lhs, rhs)
        {
        }

        protected abstract bool ComputeComparisonValue(double lhs, double rhs);

        protected override object ComputeValue(object lhsValue, object rhsValue)
        {
            if (lhsValue is Exception)
            {
                return lhsValue;
            }
            if (rhsValue is Exception)
            {
                return rhsValue;
            }
            var lhs = Convert.ToDouble(lhsValue);
            var rhs = Convert.ToDouble(rhsValue);
            return ComputeComparisonValue(lhs, rhs);
        }

        public static ComparisonExpression Create(string token, IAstNode lhs, IAstNode rhs)
        {
            return token switch
            {
                "<" => new LtComparison(lhs, rhs),
                "<=" => new LeqComparison(lhs, rhs),
                "==" => new EqComparison(lhs, rhs),
                "!=" or "<>" => new NeqComparison(lhs, rhs),
                ">=" => new GeqComparison(lhs, rhs),
                ">" => new GtComparison(lhs, rhs),
                _ => throw new Exception($"Unexpected operator {token}"),
            };
        }
    }

    class LtComparison : ComparisonExpression
    {
        public LtComparison(IAstNode lhs, IAstNode rhs) : base(lhs, rhs)
        {
        }

        protected override bool ComputeComparisonValue(double lhs, double rhs)
        {
            return lhs < rhs;
        }
        protected override string OperatorText => "<";
    }

    class LeqComparison : ComparisonExpression
    {
        public LeqComparison(IAstNode lhs, IAstNode rhs) : base(lhs, rhs)
        {
        }

        protected override bool ComputeComparisonValue(double lhs, double rhs)
        {
            return lhs <= rhs;
        }
        protected override string OperatorText => "<=";
    }

    class EqComparison : ComparisonExpression
    {
        public EqComparison(IAstNode lhs, IAstNode rhs) : base(lhs, rhs)
        {
        }

        protected override bool ComputeComparisonValue(double lhs, double rhs)
        {
            return lhs == rhs;
        }
        protected override string OperatorText => "==";
    }

    class NeqComparison : ComparisonExpression
    {
        public NeqComparison(IAstNode lhs, IAstNode rhs) : base(lhs, rhs)
        {
        }

        protected override bool ComputeComparisonValue(double lhs, double rhs)
        {
            return lhs != rhs;
        }
        protected override string OperatorText => "!=";
    }

    class GeqComparison : ComparisonExpression
    {
        public GeqComparison(IAstNode lhs, IAstNode rhs) : base(lhs, rhs)
        {
        }

        protected override bool ComputeComparisonValue(double lhs, double rhs)
        {
            return lhs >= rhs;
        }
        protected override string OperatorText => ">=";
    }

    class GtComparison : ComparisonExpression
    {
        public GtComparison(IAstNode lhs, IAstNode rhs) : base(lhs, rhs)
        {
        }

        protected override bool ComputeComparisonValue(double lhs, double rhs)
        {
            return lhs > rhs;
        }
        protected override string OperatorText => ">";
    }

    class UnaryMinusExpression : UnaryExpression<double, double>
    {
        public UnaryMinusExpression(IAstNode child) : base(child)
        {
        }

        protected override double ComputeValue(double child)
        {
            return -child;
        }

        public static IAstNode Create(IAstNode child)
        {
            return new UnaryMinusExpression(child);
        }

        public override string ToString()
        {
            return $"-{Child}";
        }
    }
}