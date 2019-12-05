using System;
using System.Text;
using System.IO;
using System.Diagnostics.CodeAnalysis;

class Utility {
    public static void Assert(bool expr)
    {
        if (false == expr)
            throw new InvalidOperationException("Error: Assertion failed.");
    }
}

[SuppressMessage("ReSharper", "InconsistentNaming")]
enum Symbol
{
    UNKNOWN = 0,
    // Simple tokens
    COMMA,
    COLON,
    SEMICOLON,
    LEFT_PARAN,
    RIGHT_PARAN,
    LEFT_SQUARE_BRACKET,
    RIGHT_SQUARE_BRACKET,
    LEFT_CURLY_BRACE,
    RIGHT_CURLY_BRACE,
    PERIOD,
    PLUS,
    MINUS,
    ASTERISK,
    SLASH,
    BACKSLASH,
    ASSIGNMENT,
    EQUAL,
    NOT_EQUAL,
    LT,
    LTE,
    GT,
    GTE,
    AMPERSAND,
    AND,
    PIPE,
    OR,
    PASCAL_ASSIGNMENT,
    // Longer tokens
    TEXT,
    TEXT_UNCLOSED,
    NUMBER,
    IDENTIFIER,
    // Reserved terminal symbols
    EPSILON,
    EOF
}

interface ILexer
{
    Token GetNextToken();
}

// default name is Yytoken, but changed to Token with %class directive
class Token
{
    public static readonly Token EPSILON = new Token(Symbol.EPSILON, string.Empty, 0, 0, 0);
    public static readonly Token EOF = new Token(Symbol.EOF, string.Empty, 0, 0, 0);

    internal Token(
        Symbol symbol,
        string text,
        int line,
        int charBegin,
        int charEnd)
    {
        Symbol = symbol;
        Text = text;
        Line = line;
        CharBegin = charBegin;
        CharEnd = charEnd;
    }

    public Symbol Symbol { get; }

    /// <summary>
    /// The lexeme (text value) recognized by the lexer.
    /// </summary>
    public string Text { get; }

    public int Line { get; }
    public int CharBegin;
    public int CharEnd;

    public override string ToString()
    {
        return "Token (" + Symbol + "): " + Text  + " (Line " + Line + ")";
    }
}

%%

%{
    private static int comment_count = 0;
%}

%line
%char
%state COMMENT

%namespace CsLexerRepl.Lexers
%class SampleLexer
%implements ILexer
%function GetNextToken
%type Token
%eof Token<Sym>.EOF
%epsilon Token<Sym>.EPSILON

ALPHA=[A-Za-z]
DIGIT=[0-9]
BACKSLASH=(\\)
NEWLINE=((\r\n?)|\n)
NON_NEWLINE_WHITE_SPACE_CHAR=[\ \t\b\012]
WHITE_SPACE_CHAR=[{NEWLINE}\ \t\b\012]
STRING_TEXT=(\\\"|[^{NEWLINE}\"]|\\{WHITE_SPACE_CHAR}+\\)*
COMMENT_TEXT=([^*/\r\n]|[^*\r\n]"/"[^*\r\n]|[^/\r\n]"*"[^/\r\n]|"*"[^/\r\n]|"/"[^*\r\n])*

%%

<YYINITIAL> "," { return (new Token(Symbol.COMMA,yytext(),yyline,yychar,yychar+1)); }
<YYINITIAL> ":" { return (new Token(Symbol.COLON,yytext(),yyline,yychar,yychar+1)); }
<YYINITIAL> ";" { return (new Token(Symbol.SEMICOLON,yytext(),yyline,yychar,yychar+1)); }
<YYINITIAL> "(" { return (new Token(Symbol.LEFT_PARAN,yytext(),yyline,yychar,yychar+1)); }
<YYINITIAL> ")" { return (new Token(Symbol.RIGHT_PARAN,yytext(),yyline,yychar,yychar+1)); }
<YYINITIAL> "[" { return (new Token(Symbol.LEFT_SQUARE_BRACKET,yytext(),yyline,yychar,yychar+1)); }
<YYINITIAL> "]" { return (new Token(Symbol.RIGHT_SQUARE_BRACKET,yytext(),yyline,yychar,yychar+1)); }
<YYINITIAL> "{" { return (new Token(Symbol.LEFT_CURLY_BRACE,yytext(),yyline,yychar,yychar+1)); }
<YYINITIAL> "}" { return (new Token(Symbol.RIGHT_CURLY_BRACE,yytext(),yyline,yychar,yychar+1)); }
<YYINITIAL> "." { return (new Token(Symbol.PERIOD,yytext(),yyline,yychar,yychar+1)); }
<YYINITIAL> "+" { return (new Token(Symbol.PLUS,yytext(),yyline,yychar,yychar+1)); }
<YYINITIAL> "-" { return (new Token(Symbol.MINUS,yytext(),yyline,yychar,yychar+1)); }
<YYINITIAL> "*" { return (new Token(Symbol.ASTERISK,yytext(),yyline,yychar,yychar+1)); }
<YYINITIAL> "/" { return (new Token(Symbol.SLASH,yytext(),yyline,yychar,yychar+1)); }
<YYINITIAL> {BACKSLASH} { return (new Token(Symbol.BACKSLASH,yytext(),yyline,yychar,yychar+1)); }
<YYINITIAL> "=" { return (new Token(Symbol.ASSIGNMENT,yytext(),yyline,yychar,yychar+1)); }
<YYINITIAL> "==" { return (new Token(Symbol.EQUAL,yytext(),yyline,yychar,yychar+1)); }
<YYINITIAL> "<>" { return (new Token(Symbol.NOT_EQUAL,yytext(),yyline,yychar,yychar+2)); }
<YYINITIAL> "<"  { return (new Token(Symbol.LT,yytext(),yyline,yychar,yychar+1)); }
<YYINITIAL> "<=" { return (new Token(Symbol.LTE,yytext(),yyline,yychar,yychar+2)); }
<YYINITIAL> ">"  { return (new Token(Symbol.GT,yytext(),yyline,yychar,yychar+1)); }
<YYINITIAL> ">=" { return (new Token(Symbol.GTE,yytext(),yyline,yychar,yychar+2)); }
<YYINITIAL> "&"  { return (new Token(Symbol.AMPERSAND,yytext(),yyline,yychar,yychar+1)); }
<YYINITIAL> "&&"  { return (new Token(Symbol.AND,yytext(),yyline,yychar,yychar+1)); }
<YYINITIAL> "|"  { return (new Token(Symbol.PIPE,yytext(),yyline,yychar,yychar+1)); }
<YYINITIAL> "||"  { return (new Token(Symbol.OR,yytext(),yyline,yychar,yychar+1)); }
<YYINITIAL> ":=" { return (new Token(Symbol.PASCAL_ASSIGNMENT,yytext(),yyline,yychar,yychar+2)); }

<YYINITIAL> {NON_NEWLINE_WHITE_SPACE_CHAR}+ { return Token.EPSILON; }

<YYINITIAL,COMMENT> [(\r\n?|\n)] { return Token.EPSILON; }

<YYINITIAL> "/*" { yybegin(COMMENT); comment_count = comment_count + 1; Token.EPSILON; }

<COMMENT> "/*" { comment_count = comment_count + 1; Token.EPSILON }
<COMMENT> "*/" {
    comment_count = comment_count - 1;
    Utility.Assert(comment_count >= 0);
    if (comment_count == 0)
    {
        yybegin(YYINITIAL);
    }
    return Token.EPSILON;
}

<COMMENT> {COMMENT_TEXT} { return Token.EPSILON; }

<YYINITIAL> \"{STRING_TEXT}\" {
    string str =  yytext().Substring(1,yytext().Length - 2);
    Utility.Assert(str.Length == yytext().Length - 2);
    return (new Token(Symbol.TEXT,str,yyline,yychar,yychar + str.Length));
}

<YYINITIAL> \"{STRING_TEXT} {
    string str =  yytext().Substring(1,yytext().Length - 1);
    Console.WriteLine("Error: Unclosed string.");
    Utility.Assert(str.Length == yytext().Length - 1);
    return (new Token(Symbol.TEXT_UNCLOSED,str,yyline,yychar,yychar + str.Length));
}

<YYINITIAL> {DIGIT}+ {
    return (new Token(Symbol.NUMBER,yytext(),yyline,yychar,yychar + yytext().Length));
}

<YYINITIAL> {ALPHA}({ALPHA}|{DIGIT}|_)* {
    return (new Token(Symbol.IDENTIFIER,yytext(),yyline,yychar,yychar + yytext().Length));
}

<YYINITIAL,COMMENT> . {
    var sb = new StringBuilder("Illegal character: <");
    string s = yytext();
    for (int i = 0; i < s.Length; i++)
    {
        if (s[i] >= 32)
            sb.Append(s[i]);
        else
        {
            sb.Append("^");
            sb.Append(Convert.ToChar(s[i]+'A'-1));
        }
    }
    sb.Append(">");
    Console.WriteLine(sb.ToString());
    Console.WriteLine("Error: Illegal character.");
    return new Token(Symbol.UNKNOWN,yytext(),yyline,yychar,yychar + yytext().Length);
}
