using System;
using sly.lexer;

namespace BravoLights.Common.Ast
{
    abstract class BooleanLogicalExpression : BinaryExpression<bool>
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

        protected override object ComputeValue(object lhsValue, object rhsValue)
        {
            if (lhsValue is bool lhs)
            {
                if (lhs == false)
                {
                    return false;
                }
            }
            if (rhsValue is bool rhs)
            {
                if (rhs == false)
                {
                    return false;
                }
            }
            if (lhsValue is Exception)
            {
                return lhsValue;
            }
            if (rhsValue is Exception)
            {
                return rhsValue;
            }

            // Neither side is false, and neither are Exceptions
            return true;
        }

        public override IAstNode Optimize()
        {
            var optimizedLeft = Lhs.Optimize();
            var optimizedRight = Rhs.Optimize();

            if (optimizedLeft is LiteralBoolNode lhsNode)
            {
                // ON AND A -> A
                // OFF AND A -> OFF
                return lhsNode.Value ? optimizedRight : LiteralBoolNode.Off;
            }
            if (optimizedRight is LiteralBoolNode rhsNode)
            {
                // A AND ON -> A
                // A AND OFF -> OFF
                return rhsNode.Value ? optimizedLeft : LiteralBoolNode.Off;
            }

            return new AndExpression(optimizedLeft, optimizedRight);
        }

        protected override string OperatorText => "AND";
    }

    class OrExpression : BooleanLogicalExpression
    {
        public OrExpression(IAstNode lhs, IAstNode rhs) : base(lhs, rhs)
        {
        }

        protected override object ComputeValue(object lhsValue, object rhsValue)
        {
            if (lhsValue is bool lhs)
            {
                if (lhs == true)
                {
                    return true;
                }
            }
            if (rhsValue is bool rhs)
            {
                if (rhs == true)
                {
                    return true;
                }
            }
            if (lhsValue is Exception)
            {
                return lhsValue;
            }
            if (rhsValue is Exception)
            {
                return rhsValue;
            }

            // Neither side is true, and neither are Exceptions
            return false;
        }

        public override IAstNode Optimize()
        {
            var optimizedLeft = Lhs.Optimize();
            var optimizedRight = Rhs.Optimize();

            if (optimizedLeft is LiteralBoolNode lhsNode)
            {
                // ON OR A -> ON
                // OFF OR A -> A
                return lhsNode.Value ? LiteralBoolNode.On : optimizedRight;
            }
            if (optimizedRight is LiteralBoolNode rhsNode)
            {
                // A OR ON -> ON
                // A OR OFF -> A
                return rhsNode.Value ? LiteralBoolNode.On : optimizedLeft;
            }

            return new OrExpression(optimizedLeft, optimizedRight);
        }

        protected override string OperatorText => "OR";
    }

    class NotExpression : UnaryExpression<bool, bool>
    {
        public NotExpression(IAstNode child) : base(child)
        {
        }

        protected override bool ComputeValue(bool child)
        {
            return !child;
        }

        public override string ToString()
        {
            return $"(NOT {Child})";
        }

        public override IAstNode Optimize()
        {
            if (Child is LiteralBoolNode node)
            {
                // NOT ON -> OFF
                // NOT OFF -> ON
                return node.Value ? LiteralBoolNode.Off : LiteralBoolNode.On;
            }

            return this;
        }
    }
}
