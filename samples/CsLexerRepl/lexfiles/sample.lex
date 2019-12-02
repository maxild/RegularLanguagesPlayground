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
enum Token
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
    IDENTIFIER
}

class Yytoken
{
    internal Yytoken(
        Token token,
        string text,
        int line,
        int charBegin,
        int charEnd)
    {
        Index = (int)token - 1; // TODO: Change this later...
        Text = text;
        Line = line;
        CharBegin = charBegin;
        CharEnd = charEnd;
    }

    internal Yytoken(
        int index,
        string text,
        int line,
        int charBegin,
        int charEnd)
    {
        Index = index;
        Text = text;
        Line = line;
        CharBegin = charBegin;
        CharEnd = charEnd;
    }

    public int Index { get; }

    public string Id => Enum.GetName(typeof(Token), Index + 1) ?? "UNKNOWN";

    /// <summary>
    /// The lexeme (text value) recognized by the lexer.
    /// </summary>
    public string Text { get; }

    public int Line { get; }
    public int CharBegin;
    public int CharEnd;

    public override string ToString()
    {
        return "Token (#" + Index + "," + Id + "): " + Text  + " (Line " + Line + ")";
    }
}

%%

%{
    private static int comment_count = 0;
%}

%line
%char
%state COMMENT

ALPHA=[A-Za-z]
DIGIT=[0-9]
BACKSLASH=(\\)
NEWLINE=((\r\n)|\n)
NON_NEWLINE_WHITE_SPACE_CHAR=[\ \t\b\012]
WHITE_SPACE_CHAR=[{NEWLINE}\ \t\b\012]
STRING_TEXT=(\\\"|[^{NEWLINE}\"]|\\{WHITE_SPACE_CHAR}+\\)*
COMMENT_TEXT=([^*/\r\n]|[^*\r\n]"/"[^*\r\n]|[^/\r\n]"*"[^/\r\n]|"*"[^/\r\n]|"/"[^*\r\n])*

%%

<YYINITIAL> "," { return (new Yytoken(Token.COMMA,yytext(),yyline,yychar,yychar+1)); }
<YYINITIAL> ":" { return (new Yytoken(Token.COLON,yytext(),yyline,yychar,yychar+1)); }
<YYINITIAL> ";" { return (new Yytoken(Token.SEMICOLON,yytext(),yyline,yychar,yychar+1)); }
<YYINITIAL> "(" { return (new Yytoken(Token.LEFT_PARAN,yytext(),yyline,yychar,yychar+1)); }
<YYINITIAL> ")" { return (new Yytoken(Token.RIGHT_PARAN,yytext(),yyline,yychar,yychar+1)); }
<YYINITIAL> "[" { return (new Yytoken(Token.LEFT_SQUARE_BRACKET,yytext(),yyline,yychar,yychar+1)); }
<YYINITIAL> "]" { return (new Yytoken(Token.RIGHT_SQUARE_BRACKET,yytext(),yyline,yychar,yychar+1)); }
<YYINITIAL> "{" { return (new Yytoken(Token.LEFT_CURLY_BRACE,yytext(),yyline,yychar,yychar+1)); }
<YYINITIAL> "}" { return (new Yytoken(Token.RIGHT_CURLY_BRACE,yytext(),yyline,yychar,yychar+1)); }
<YYINITIAL> "." { return (new Yytoken(Token.PERIOD,yytext(),yyline,yychar,yychar+1)); }
<YYINITIAL> "+" { return (new Yytoken(Token.PLUS,yytext(),yyline,yychar,yychar+1)); }
<YYINITIAL> "-" { return (new Yytoken(Token.MINUS,yytext(),yyline,yychar,yychar+1)); }
<YYINITIAL> "*" { return (new Yytoken(Token.ASTERISK,yytext(),yyline,yychar,yychar+1)); }
<YYINITIAL> "/" { return (new Yytoken(Token.SLASH,yytext(),yyline,yychar,yychar+1)); }
<YYINITIAL> {BACKSLASH} { return (new Yytoken(Token.BACKSLASH,yytext(),yyline,yychar,yychar+1)); }
<YYINITIAL> "=" { return (new Yytoken(Token.ASSIGNMENT,yytext(),yyline,yychar,yychar+1)); }
<YYINITIAL> "==" { return (new Yytoken(Token.EQUAL,yytext(),yyline,yychar,yychar+1)); }
<YYINITIAL> "<>" { return (new Yytoken(Token.NOT_EQUAL,yytext(),yyline,yychar,yychar+2)); }
<YYINITIAL> "<"  { return (new Yytoken(Token.LT,yytext(),yyline,yychar,yychar+1)); }
<YYINITIAL> "<=" { return (new Yytoken(Token.LTE,yytext(),yyline,yychar,yychar+2)); }
<YYINITIAL> ">"  { return (new Yytoken(Token.GT,yytext(),yyline,yychar,yychar+1)); }
<YYINITIAL> ">=" { return (new Yytoken(Token.GTE,yytext(),yyline,yychar,yychar+2)); }
<YYINITIAL> "&"  { return (new Yytoken(Token.AMPERSAND,yytext(),yyline,yychar,yychar+1)); }
<YYINITIAL> "&&"  { return (new Yytoken(Token.AND,yytext(),yyline,yychar,yychar+1)); }
<YYINITIAL> "|"  { return (new Yytoken(Token.PIPE,yytext(),yyline,yychar,yychar+1)); }
<YYINITIAL> "||"  { return (new Yytoken(Token.OR,yytext(),yyline,yychar,yychar+1)); }
<YYINITIAL> ":=" { return (new Yytoken(Token.PASCAL_ASSIGNMENT,yytext(),yyline,yychar,yychar+2)); }

<YYINITIAL> {NON_NEWLINE_WHITE_SPACE_CHAR}+ { return null; }

<YYINITIAL,COMMENT> [(\r\n?|\n)] { return null; }

<YYINITIAL> "/*" { yybegin(COMMENT); comment_count = comment_count + 1; return null; }

<COMMENT> "/*" { comment_count = comment_count + 1; return null; }
<COMMENT> "*/" {
    comment_count = comment_count - 1;
    Utility.Assert(comment_count >= 0);
    if (comment_count == 0)
    {
        yybegin(YYINITIAL);
    }
    return null;
}

<COMMENT> {COMMENT_TEXT} { return null; }

<YYINITIAL> \"{STRING_TEXT}\" {
    string str =  yytext().Substring(1,yytext().Length - 2);
    Utility.Assert(str.Length == yytext().Length - 2);
    return (new Yytoken(Token.TEXT,str,yyline,yychar,yychar + str.Length));
}

<YYINITIAL> \"{STRING_TEXT} {
    string str =  yytext().Substring(1,yytext().Length - 1);
    Console.WriteLine("Error: Unclosed string.");
    Utility.Assert(str.Length == yytext().Length - 1);
    return (new Yytoken(Token.TEXT_UNCLOSED,str,yyline,yychar,yychar + str.Length));
}

<YYINITIAL> {DIGIT}+ {
    return (new Yytoken(Token.NUMBER,yytext(),yyline,yychar,yychar + yytext().Length));
}

<YYINITIAL> {ALPHA}({ALPHA}|{DIGIT}|_)* {
    return (new Yytoken(Token.IDENTIFIER,yytext(),yyline,yychar,yychar + yytext().Length));
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
    return new Yytoken(Token.UNKNOWN,yytext(),yyline,yychar,yychar + yytext().Length);
}
