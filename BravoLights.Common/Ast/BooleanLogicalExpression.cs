using System;
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
            switch (token.Value)
            {
                case "&&":
                case "AND":
                    return new AndExpression(lhs, rhs);
                case "||":
                case "OR":
                    return new OrExpression(lhs, rhs);
                default:
                    throw new Exception($"Unexpected operator {token.Value}");
            }
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
}
