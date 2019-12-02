using System;
using System.IO;
using YyNameSpace; // Can we change this to CsLexerRepl.Lexers?

namespace CsLexerRepl
{
    class Program
    {
        static int Main(string[] args)
        {
            TextReader input;
            if (args.Length > 0)
            {
                input = new StreamReader(new FileStream(args[0], FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 8192));
                Yylex yy = new Yylex(input);
                Yytoken t;
                while ((t = yy.yylex()) != null)
                    Console.WriteLine(t);
            }
            else
            {
                Console.WriteLine("Press 'q' to quit the REPL...");
                // repl
                while (true)
                {
                    Console.Write("> ");
                    var s = Console.ReadLine() ?? string.Empty;
                    if (s == "q") break;
                    input = new StringReader(s);
                    Yylex yy = new Yylex(input);
                    Yytoken t;
                    while ((t = yy.yylex()) != null)
                        Console.WriteLine(t);
                }
            }

            return 0;
        }
    }
}
