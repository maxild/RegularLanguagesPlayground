using System;
using System.IO;
using CsLexerRepl.Lexers;

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
                var lexer = new SampleLexer(input);
                while (true)
                {
                    var t = lexer.GetNextToken();
                    if (t.Symbol == Symbol.EOF) break;
                    Console.WriteLine(t);
                }
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
                    var lexer = new SampleLexer(input);
                    while (true)
                    {
                        var t = lexer.GetNextToken();
                        if (t.Symbol == Symbol.EOF) break;
                        Console.WriteLine(t);
                    }
                }
            }

            return 0;
        }
    }
}
