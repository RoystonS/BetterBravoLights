using System;
using System.Collections.Generic;
using sly.lexer;

namespace BravoLights.Common.Ast
{
    abstract class BooleanLogicalExpression : BinaryExpression<bool, bool>
    {
        protected BooleanLogicalExpression(IAstNode lhs, IAstNode rhs) : base(lhs, rhs)
        {
        }

        public static BooleanLogicalExpression Create(IAstNode lhs, Token<ExpressionToken> token, IAstNode rhs)
        {
            return token.Value switch
            {
                "&&" or "AND" => new AndExpression(lhs, rhs),
                "||" or "OR" => new OrExpression(lhs, rhs),
                _ => throw new Exception($"Unexpected operator {token.Value}"),
            };
        }
    }

    class AndExpression : BooleanLogicalExpression
    {
        public AndExpression(IAstNode lhs, IAstNode rhs) : base(lhs, rhs)
        {
        }

        protected override bool ComputeValue(bool lhs, bool rhs)
        {
            return lhs && rhs;
        }
        protected override string OperatorText => "AND";
    }

    class OrExpression : BooleanLogicalExpression
    {
        public OrExpression(IAstNode lhs, IAstNode rhs) : base(lhs, rhs)
        {
        }

        protected override bool ComputeValue(bool lhs, bool rhs)
        {
            return lhs || rhs;
        }
        protected override string OperatorText => "OR";
    }

    class NotExpression : UnaryExpression<bool, bool>
    {
        public NotExpression(IAstNode child) : base(child)
        {
        }

        protected override string OperatorText => "NOT";

        protected override bool ComputeValue(bool child)
        {
            return !child;
        }
    }
}
