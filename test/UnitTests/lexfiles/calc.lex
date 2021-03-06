using System;
using System.Text;
using System.IO;
using System.Diagnostics.CodeAnalysis;
using AutomataLib;
using ContextFreeGrammar.Lexers;

[SuppressMessage("ReSharper", "InconsistentNaming")]
enum Sym
{
  // Hidden token kinds have negative values
  NIL = -2,   // Lexical error (syntax error)
  EPS = -1,   // Epsilon (empty/hidden) token
  // All non-hidden token kinds are sequentially ordered 0,1,2,...,N-1
  EOF = 0,
  LPARAN,
  RPARAN,
  PLUS,
  MINUS,
  NUM,
  ID
}

%%

%line
%char

%namespace UnitTests.Lexers
%class CalcLexer
%implements ILexer<Token<Sym>>
%function GetNextToken
%type Token<Sym>

%epsilon Token.Epsilon<Sym>()
%eof Token.Eof<Sym>()

ALPHA=[A-Za-z]
DIGIT=[0-9]
NEWLINE=((\r\n?)|\n)
NON_NEWLINE_WHITE_SPACE_CHAR=[\ \t\b\012]
WHITE_SPACE_CHAR=[{NEWLINE}\ \t\b\012]

%%

<YYINITIAL> "(" { return new Token<Sym>(Sym.LPARAN, yytext()); }
<YYINITIAL> ")" { return new Token<Sym>(Sym.RPARAN, yytext()); }
<YYINITIAL> "+" { return new Token<Sym>(Sym.PLUS, yytext()); }
<YYINITIAL> "-" { return new Token<Sym>(Sym.MINUS, yytext()); }

<YYINITIAL> {NON_NEWLINE_WHITE_SPACE_CHAR}+ { return Token.Epsilon<Sym>(); }
<YYINITIAL> {NEWLINE}+ { return Token.Epsilon<Sym>(); }

<YYINITIAL> {DIGIT}+                    { return new Token<Sym>(Sym.NUM, yytext()); }
<YYINITIAL> {ALPHA}({ALPHA}|{DIGIT}|_)* { return new Token<Sym>(Sym.ID, yytext()); }

<YYINITIAL> . {
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
  return new Token<Sym>(Sym.NIL, yytext());
}
