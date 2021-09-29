using System;
using sly.lexer;

namespace BravoLights.Ast
{
    /// <summary>
    /// A binary expression such as 'X < Y' or 'X == Y', which produces a boolean from two other numbers and an operator.
    /// </summary>
    abstract class ComparisonExpression : BinaryExpression<double, bool>
    {
        protected ComparisonExpression(IAstNode lhs, IAstNode rhs) : base(lhs, rhs)
        {
        }

        public static ComparisonExpression Create(IAstNode lhs, Token<ExpressionToken> token, IAstNode rhs)
        {
            switch (token.Value)
            {
                case "<":
                    return new LtComparison(lhs, rhs);
                case "<=":
                    return new LeqComparison(lhs, rhs);
                case "==":
                    return new EqComparison(lhs, rhs);
                case "!=":
                case "<>":
                    return new NeqComparison(lhs, rhs);
                case ">=":
                    return new GeqComparison(lhs, rhs);
                case ">":
                    return new GtComparison(lhs, rhs);
                default:
                    throw new Exception($"Unexpected operator {token.Value}");
            }
        }
    }

    class LtComparison : ComparisonExpression
    {
        public LtComparison(IAstNode lhs, IAstNode rhs) : base(lhs, rhs)
        {
        }

        protected override bool ComputeValue(double lhs, double rhs)
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

        protected override bool ComputeValue(double lhs, double rhs)
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

        protected override bool ComputeValue(double lhs, double rhs)
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

        protected override bool ComputeValue(double lhs, double rhs)
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

        protected override bool ComputeValue(double lhs, double rhs)
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

        protected override bool ComputeValue(double lhs, double rhs)
        {
            return lhs > rhs;
        }
        protected override string OperatorText => ">";
    }
}