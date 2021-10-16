﻿using System;
using sly.lexer;

namespace BravoLights.Common.Ast
{

    enum NumericOperator
    {
        Plus,
        Minus,
        Times,
        Divide
    }

    /// <summary>
    /// A binary expression such as 'X + Y' or 'X / Y', which produces a number from two other numbers and an operator.
    /// </summary>
    abstract class BinaryNumericExpression : BinaryExpression<double, double>
    {
        protected BinaryNumericExpression(IAstNode lhs, IAstNode rhs) : base(lhs, rhs)
        {
        }

        public static BinaryNumericExpression Create(IAstNode lhs, Token<ExpressionToken> op, IAstNode rhs)
        {
            return op.Value switch
            {
                "+" => new PlusExpression(lhs, rhs),
                "-" => new MinusExpression(lhs, rhs),
                "*" => new TimesExpression(lhs, rhs),
                "/" => new DivideExpression(lhs, rhs),
                _ => throw new Exception($"Unexpected operator: {op.Value}"),
            };
        }
    }

    class PlusExpression : BinaryNumericExpression
    {
        public PlusExpression(IAstNode lhs, IAstNode rhs) : base(lhs, rhs)
        {
        }

        protected override string OperatorText => "+";

        protected override double ComputeValue(double lhs, double rhs)
        {
            return lhs + rhs;
        }
    }

    class MinusExpression : BinaryNumericExpression
    {
        public MinusExpression(IAstNode lhs, IAstNode rhs) : base(lhs, rhs)
        {
        }

        protected override string OperatorText => "-";

        protected override double ComputeValue(double lhs, double rhs)
        {
            return lhs - rhs;
        }
    }

    class TimesExpression : BinaryNumericExpression
    {
        public TimesExpression(IAstNode lhs, IAstNode rhs) : base(lhs, rhs)
        {
        }

        protected override string OperatorText => "*";

        protected override double ComputeValue(double lhs, double rhs)
        {
            return lhs * rhs;
        }
    }

    class DivideExpression : BinaryNumericExpression
    {
        public DivideExpression(IAstNode lhs, IAstNode rhs) : base(lhs, rhs)
        {
        }

        protected override string OperatorText => "/";

        protected override double ComputeValue(double lhs, double rhs)
        {
            return lhs / rhs;
        }
    }
}