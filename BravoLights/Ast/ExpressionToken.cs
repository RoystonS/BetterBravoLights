using sly.lexer;

namespace BravoLights.Ast
{
    public enum ExpressionToken
    {
        [Lexeme("L:[A-Za-z0-9_]+")]
        LVAR = 50,

        [Lexeme("OFFSET:[0-9A-F]+:(FLOAT|INT)[1248]")]
        OFFSET = 51,

        [Lexeme("A:[:A-Za-z0-9 ]+,\\s*([A-Za-z0-9 ]+)")]
        SIMVAR = 52,

        [Lexeme("OFF")]
        OFF = 0,

        [Lexeme("ON")]
        ON = 1,

        // Hex/decimal prefix?
        [Lexeme("0[xX][0-9a-fA-F]+")]
        HEX_NUMBER = 2,

        [Lexeme("[0-9]+(\\.[0-9]+)?")]
        DECIMAL_NUMBER = 3,

        [Lexeme("\\+")]
        PLUS = 4,

        [Lexeme("-")]
        MINUS = 5,

        [Lexeme("\\*")]
        TIMES = 6,

        [Lexeme("/")]
        DIVIDE = 7,

        [Lexeme("[ \\t]+", isSkippable: true)]
        WHITESPACE = 20,

        [Lexeme("(\\|\\|)|(OR)")]
        OR = 10,
        [Lexeme("(&&)|(AND)")]
        AND = 11,

        [Lexeme("(<=?)|(==)|(!=)|(>=?)")]
        COMPARISON = 13,

        [Lexeme("\\(")]
        LPAREN = 30,

        [Lexeme("\\)")]
        RPAREN = 31,
    }

}
