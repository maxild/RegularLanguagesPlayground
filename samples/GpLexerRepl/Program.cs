using System;
using System.IO;
using System.Text;
using LexScanner;

namespace GpLexerRepl
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                string path = args[0];
                try
                {
                    int tok;
                    var file = new FileStream(path, FileMode.Open);

                    // File vs Memory
                    var scanner = new Scanner(file);
                    Console.WriteLine("File: " + path);
                    do
                    {
                        tok = scanner.yylex();
                    } while (tok > (int)Tokens.EOF);
                }
                catch (IOException)
                {
                    Console.WriteLine("File " + path + " not found");
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
                    byte[] byteArray = Encoding.ASCII.GetBytes(s);
                    var scanner = new Scanner(new MemoryStream(byteArray));
                    while (true)
                    {
                        int tok = scanner.yylex();
                        if (tok == (int) Tokens.EOF) break;
                    }
                }
            }

            Console.Write("Total Lines: " + Scanner.lineTot);
            Console.Write(", Words: " + Scanner.wordTot);
            Console.Write(", Ints: " + Scanner.intTot);
            Console.WriteLine(", Floats: " + Scanner.fltTot);
        }
    }
}
