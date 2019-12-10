namespace UnitTests.Lexers
{
using System;
using System.Text;
using System.IO;
using System.Diagnostics.CodeAnalysis;
using AutomataLib;
using ContextFreeGrammar.Lexers;
[SuppressMessage("ReSharper", "InconsistentNaming")]
enum Sym
{
  EOF = -1, // EOF
  EPS = 0,  // Empty Symbol
  LPARAN,
  RPARAN,
  PLUS,
  MINUS,
  NUM,
  ID,
  ERROR
}
/* test */


internal class CalcLexer : ILexer<Token<Sym>>
{

#region constants
    
    const int YY_BUFFER_SIZE = 512;
    const int YY_F = -1;
    const int YY_NO_STATE = -1;
    const int YY_NOT_ACCEPT = 0;
    const int YY_START = 1;
    const int YY_END = 2;
    const int YY_NO_ANCHOR = 4;
    const int YY_BOL = 128;
    const int YY_EOF = 129;
#endregion

    delegate Token<Sym> AcceptMethod();
    AcceptMethod[] accept_dispatch;


    #region private members
    TextReader yy_reader;
    int yy_buffer_index;
    int yy_buffer_read;
    int yy_buffer_start;
    int yy_buffer_end;
    char[] yy_buffer = new char[YY_BUFFER_SIZE];
    int yychar;
    int yyline;
    bool yy_at_bol = true;
    int yy_lexical_state = YYINITIAL;
    #endregion

    #region constructors

    internal  CalcLexer(TextReader reader) : this()
    {
        if (reader == null)
            throw new ApplicationException("Error: Bad input stream initializer.");
        yy_reader = reader;
    }

    internal  CalcLexer(FileStream instream) : this()
    {
        if (instream == null)
            throw new ApplicationException("Error: Bad input stream initializer.");
        yy_reader = new StreamReader(instream);
    }

    CalcLexer()
    {
        actionInit();
        userInit();
    }

    #endregion

    #region action init
    void actionInit()
    {

    accept_dispatch = new AcceptMethod[]
        {

            null,

            null,

            new AcceptMethod(this.Accept_2),

            new AcceptMethod(this.Accept_3),

            new AcceptMethod(this.Accept_4),

            new AcceptMethod(this.Accept_5),

            new AcceptMethod(this.Accept_6),

            new AcceptMethod(this.Accept_7),

            new AcceptMethod(this.Accept_8),

            new AcceptMethod(this.Accept_9),

            new AcceptMethod(this.Accept_10),

            new AcceptMethod(this.Accept_11),

        };
    }
    #endregion

    #region user init
    void userInit()
    {
    // no user init
    }
    #endregion

    #region action methods

    Token<Sym> Accept_2()
    { return new Token<Sym>(Sym.LPARAN, yytext()); }

    Token<Sym> Accept_3()
    { return new Token<Sym>(Sym.RPARAN, yytext()); }

    Token<Sym> Accept_4()
    { return new Token<Sym>(Sym.PLUS, yytext()); }

    Token<Sym> Accept_5()
    { return new Token<Sym>(Sym.MINUS, yytext()); }

    Token<Sym> Accept_6()
    { return Token<Sym>.EPS; }

    Token<Sym> Accept_7()
    { return Token<Sym>.EPS; }

    Token<Sym> Accept_8()
    { return new Token<Sym>(Sym.NUM, yytext()); }

    Token<Sym> Accept_9()
    { return new Token<Sym>(Sym.ID, yytext()); }

    Token<Sym> Accept_10()
    {
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
  return new Token<Sym>(Sym.ERROR, yytext());
}

    Token<Sym> Accept_11()
    { return Token<Sym>.EPS; }

    #endregion
private const int YYINITIAL = 0;
private static int[] yy_state_dtrans = new int[] 
  {   0
  };
#region helpers
private void yybegin (int state)
  {
  yy_lexical_state = state;
  }

private char yy_advance ()
  {
  int next_read;
  int i;
  int j;

  if (yy_buffer_index < yy_buffer_read)
    {
    return yy_buffer[yy_buffer_index++];
    }

  if (0 != yy_buffer_start)
    {
    i = yy_buffer_start;
    j = 0;
    while (i < yy_buffer_read)
      {
      yy_buffer[j] = yy_buffer[i];
      i++;
      j++;
      }
    yy_buffer_end = yy_buffer_end - yy_buffer_start;
    yy_buffer_start = 0;
    yy_buffer_read = j;
    yy_buffer_index = j;
    next_read = yy_reader.Read(yy_buffer,yy_buffer_read,
                  yy_buffer.Length - yy_buffer_read);
    if (next_read <= 0)
      {
      return (char) YY_EOF;
      }
    yy_buffer_read = yy_buffer_read + next_read;
    }
  while (yy_buffer_index >= yy_buffer_read)
    {
    if (yy_buffer_index >= yy_buffer.Length)
      {
      yy_buffer = yy_double(yy_buffer);
      }
    next_read = yy_reader.Read(yy_buffer,yy_buffer_read,
                  yy_buffer.Length - yy_buffer_read);
    if (next_read <= 0)
      {
      return (char) YY_EOF;
      }
    yy_buffer_read = yy_buffer_read + next_read;
    }
  return yy_buffer[yy_buffer_index++];
  }
private void yy_move_end ()
  {
  if (yy_buffer_end > yy_buffer_start && 
      '\n' == yy_buffer[yy_buffer_end-1])
    yy_buffer_end--;
  if (yy_buffer_end > yy_buffer_start &&
      '\r' == yy_buffer[yy_buffer_end-1])
    yy_buffer_end--;
  }
private bool yy_last_was_cr=false;
private void yy_mark_start ()
  {
  int i;
  for (i = yy_buffer_start; i < yy_buffer_index; i++)
    {
    if (yy_buffer[i] == '\n' && !yy_last_was_cr)
      {
      yyline++;
      }
    if (yy_buffer[i] == '\r')
      {
      yyline++;
      yy_last_was_cr=true;
      }
    else
      {
      yy_last_was_cr=false;
      }
    }
  yychar = yychar + yy_buffer_index - yy_buffer_start;
  yy_buffer_start = yy_buffer_index;
  }
private void yy_mark_end ()
  {
  yy_buffer_end = yy_buffer_index;
  }
private void yy_to_mark ()
  {
  yy_buffer_index = yy_buffer_end;
  yy_at_bol = (yy_buffer_end > yy_buffer_start) &&
    (yy_buffer[yy_buffer_end-1] == '\r' ||
    yy_buffer[yy_buffer_end-1] == '\n');
  }
internal string yytext()
  {
  return (new string(yy_buffer,
                yy_buffer_start,
                yy_buffer_end - yy_buffer_start)
         );
  }
private int yylength ()
  {
  return yy_buffer_end - yy_buffer_start;
  }
private char[] yy_double (char[] buf)
  {
  int i;
  char[] newbuf;
  newbuf = new char[2*buf.Length];
  for (i = 0; i < buf.Length; i++)
    {
    newbuf[i] = buf[i];
    }
  return newbuf;
  }
private const int YY_E_INTERNAL = 0;
private const int YY_E_MATCH = 1;
private static string[] yy_error_string = new string[]
  {
  "Error: Internal error.\n",
  "Error: Unmatched input.\n"
  };
private void yy_error (int code,bool fatal)
  {
  System.Console.Write(yy_error_string[code]);
  if (fatal)
    {
    throw new System.ApplicationException("Fatal Error.\n");
    }
  }
#endregion

    #region tables

    static int[] yy_acpt = new int[]
    {
    /* 0 */   YY_NOT_ACCEPT,
    /* 1 */   YY_NO_ANCHOR,
    /* 2 */   YY_NO_ANCHOR,
    /* 3 */   YY_NO_ANCHOR,
    /* 4 */   YY_NO_ANCHOR,
    /* 5 */   YY_NO_ANCHOR,
    /* 6 */   YY_NO_ANCHOR,
    /* 7 */   YY_NO_ANCHOR,
    /* 8 */   YY_NO_ANCHOR,
    /* 9 */   YY_NO_ANCHOR,
    /* 10 */   YY_NO_ANCHOR,
    /* 11 */   YY_NO_ANCHOR
    };

    static int[] yy_cmap = new int[]
    {
    /* 0-7 */ 11, 11, 11, 11, 11, 11, 11, 11, 
    /* 8-15 */ 5, 5, 7, 11, 11, 6, 11, 11, 
    /* 16-23 */ 11, 11, 11, 11, 11, 11, 11, 11, 
    /* 24-31 */ 11, 11, 11, 11, 11, 11, 11, 11, 
    /* 32-39 */ 5, 11, 11, 11, 11, 11, 11, 11, 
    /* 40-47 */ 1, 2, 11, 3, 11, 4, 11, 11, 
    /* 48-55 */ 8, 8, 8, 8, 8, 8, 8, 8, 
    /* 56-63 */ 8, 8, 11, 11, 11, 11, 11, 11, 
    /* 64-71 */ 11, 9, 9, 9, 9, 9, 9, 9, 
    /* 72-79 */ 9, 9, 9, 9, 9, 9, 9, 9, 
    /* 80-87 */ 9, 9, 9, 9, 9, 9, 9, 9, 
    /* 88-95 */ 9, 9, 9, 11, 11, 11, 11, 10, 
    /* 96-103 */ 11, 9, 9, 9, 9, 9, 9, 9, 
    /* 104-111 */ 9, 9, 9, 9, 9, 9, 9, 9, 
    /* 112-119 */ 9, 9, 9, 9, 9, 9, 9, 9, 
    /* 120-127 */ 9, 9, 9, 11, 11, 11, 11, 11, 
    /* 128-135 */ 0, 0
    };

    static int[] yy_rmap = new int[]
    {
    /* 0-7 */ 0, 1, 1, 1, 1, 1, 2, 3, 
    /* 8-15 */ 4, 5, 1, 6
    };

    static int[,] yy_nxt = new int[,]
    {
        {
        /* 0-7 */ 1, 2, 3, 4, 5, 6, 7, 11, 
        /* 8-15 */ 8, 9, 10, 10
        },
        {
        /* 0-7 */ -1, -1, -1, -1, -1, -1, -1, -1, 
        /* 8-15 */ -1, -1, -1, -1
        },
        {
        /* 0-7 */ -1, -1, -1, -1, -1, 6, -1, 6, 
        /* 8-15 */ -1, -1, -1, -1
        },
        {
        /* 0-7 */ -1, -1, -1, -1, -1, -1, 7, 7, 
        /* 8-15 */ -1, -1, -1, -1
        },
        {
        /* 0-7 */ -1, -1, -1, -1, -1, -1, -1, -1, 
        /* 8-15 */ 8, -1, -1, -1
        },
        {
        /* 0-7 */ -1, -1, -1, -1, -1, -1, -1, -1, 
        /* 8-15 */ 9, 9, 9, -1
        },
        {
        /* 0-7 */ -1, -1, -1, -1, -1, 6, 7, 11, 
        /* 8-15 */ -1, -1, -1, -1
        }
    };

    #endregion

    #region driver
    public Token<Sym> GetNextToken()
    {
    char yy_lookahead;
    int yy_anchor = YY_NO_ANCHOR;
    int yy_state = yy_state_dtrans[yy_lexical_state];
    int yy_next_state = YY_NO_STATE;
    int yy_last_accept_state = YY_NO_STATE;
    bool yy_initial = true;
    int yy_this_accept;

    yy_mark_start();
    yy_this_accept = yy_acpt[yy_state];
    if (YY_NOT_ACCEPT != yy_this_accept)
    {
        yy_last_accept_state = yy_state;
        yy_mark_end();
    }

    // begin_str

    while (true)
    {
        if (yy_initial && yy_at_bol)
        {
            yy_lookahead = (char)YY_BOL;
        }
        else
        {
            yy_lookahead = yy_advance();
        }

        yy_next_state = yy_nxt[yy_rmap[yy_state], yy_cmap[yy_lookahead]];

        // state_str

        if (YY_EOF == yy_lookahead && yy_initial)
        {
            // EOF_Test()
                    return Token<Sym>.EOF;
 
        }

        if (YY_F != yy_next_state)
        {
            yy_state = yy_next_state;
            yy_initial = false;
            yy_this_accept = yy_acpt[yy_state];
            if (YY_NOT_ACCEPT != yy_this_accept)
            {
                yy_last_accept_state = yy_state;
                yy_mark_end();
            }
        }
        else
        {
            if (YY_NO_STATE == yy_last_accept_state)
            {
                throw new ApplicationException("Lexical Error: Unmatched Input.");
            }
            else
            {
                yy_anchor = yy_acpt[yy_last_accept_state];
                if (0 != (YY_END & yy_anchor))
                {
                    yy_move_end();
                }

                yy_to_mark();
                if (yy_last_accept_state< 0)
                {
                    if (yy_last_accept_state< 12) // spec.accept_list.Count
                        yy_error(YY_E_INTERNAL, false);
                }
                else
                {
                    AcceptMethod m = accept_dispatch[yy_last_accept_state];
                    if (m != null)
                    {
                        var tmp = m(); // spec.type_name
                        if (tmp != Token<Sym>.EPS)
                            return tmp;
                    }
                }

                yy_initial = true;
                yy_state = yy_state_dtrans[yy_lexical_state];
                yy_next_state = YY_NO_STATE;
                yy_last_accept_state = YY_NO_STATE;
                yy_mark_start();
                yy_this_accept = yy_acpt[yy_state];
                if (YY_NOT_ACCEPT != yy_this_accept)
                {
                    yy_last_accept_state = yy_state;
                    yy_mark_end();
                }
            }
        }
    }
    }
    #endregion

    }
}
