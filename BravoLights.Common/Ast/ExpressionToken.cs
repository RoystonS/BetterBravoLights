
using Superpower.Display;

namespace BravoLights.Common.Ast
{
    public enum ExpressionToken
    {
        LVAR = 50,
        SIMVAR = 52,
        DCS_VAR = 53,
        
        OFF = 0,
        ON = 1,

        HEX_NUMBER = 2,
        DECIMAL_NUMBER = 3,

        PLUS = 4,
        MINUS = 5,
        TIMES = 6,
        DIVIDE = 7,

        [Token(Description = "bitwise OR '|'")]
        BITWISE_OR = 8,
        
        [Token(Description = "bitwise AND '&'")]
        BITWISE_AND = 9,
        WHITESPACE = 20,

        [Token(Description = "logical OR")]
        LOGICAL_OR = 10,
        
        [Token(Description = "logical AND")]
        LOGICAL_AND = 11,

        [Token(Description = "logical NOT")]
        NOT = 12,
        
        [Token(Description = "numeric comparison")]
        COMPARISON = 13,

        [Token(Description = "opening parenthesis '('")]
        LPAREN = 30,
        
        [Token(Description = "closing parenthesis ')'")]
        RPAREN = 31,
    }
}
