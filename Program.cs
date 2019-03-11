using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.IsolatedStorage;
using System.Reflection;

namespace RegExpToDfa
{
    class Program
    {
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public static void Main()
        {
            // TestNFA: Trying the RE->NFA->DFA translation on three regular expressions
            Regex reA = new Sym("a");
            Regex reB = new Sym("b");

            // (a|b)*
            Regex reA_Plus_reB_Star = new Star(new Alt(reA, reB));

            BuildAndShow("dfa0.dot", reA_Plus_reB_Star);

            //
            // epsilon-NFA accepting accepting decimal numbers
            //

            var nfa = new Nfa(0, 5);

            // TODO: Because we do not support ranges let d be digit

            // sign
            nfa.AddTrans(0, null, 1);
            nfa.AddTrans(0, "+", 1);
            nfa.AddTrans(0, "-", 1);
            // optional digits [0-9] before decimal point
            nfa.AddTrans(1, "d", 1);
            //nfa.AddTrans(1, "1", 1);
            //nfa.AddTrans(1, "2", 1);
            //nfa.AddTrans(1, "3", 1);
            //nfa.AddTrans(1, "4", 1);
            //nfa.AddTrans(1, "5", 1);
            //nfa.AddTrans(1, "6", 1);
            //nfa.AddTrans(1, "7", 1);
            //nfa.AddTrans(1, "8", 1);
            //nfa.AddTrans(1, "9", 1);
            // decimal point before mandatory digit(s)
            nfa.AddTrans(1, ".", 2);
            // digit after state 2
            nfa.AddTrans(2, "d", 3);
            //nfa.AddTrans(2, "1", 3);
            //nfa.AddTrans(2, "2", 3);
            //nfa.AddTrans(2, "3", 3);
            //nfa.AddTrans(2, "4", 3);
            //nfa.AddTrans(2, "5", 3);
            //nfa.AddTrans(2, "6", 3);
            //nfa.AddTrans(2, "7", 3);
            //nfa.AddTrans(2, "8", 3);
            //nfa.AddTrans(2, "9", 3);
            // digit before decimal point
            nfa.AddTrans(1, "d", 4);
            //nfa.AddTrans(1, "1", 4);
            //nfa.AddTrans(1, "2", 4);
            //nfa.AddTrans(1, "3", 4);
            //nfa.AddTrans(1, "4", 4);
            //nfa.AddTrans(1, "5", 4);
            //nfa.AddTrans(1, "6", 4);
            //nfa.AddTrans(1, "7", 4);
            //nfa.AddTrans(1, "8", 4);
            //nfa.AddTrans(1, "9", 4);
            // decimal point after mandatory digit(s)
            nfa.AddTrans(4, ".", 3);
            // optional digits [0-9] after decimal point
            nfa.AddTrans(3, "d", 3);
            //nfa.AddTrans(3, "1", 3);
            //nfa.AddTrans(3, "2", 3);
            //nfa.AddTrans(3, "3", 3);
            //nfa.AddTrans(3, "4", 3);
            //nfa.AddTrans(3, "5", 3);
            //nfa.AddTrans(3, "6", 3);
            //nfa.AddTrans(3, "7", 3);
            //nfa.AddTrans(3, "8", 3);
            //nfa.AddTrans(3, "9", 3);
            // epsilon-transition to accepting/final state
            nfa.AddTrans(3, null, 5);

            Dfa dfa = nfa.ToDfa();

            dfa.WriteDot(GetPath("dfa_example.dot"));

            //
            // Keyword search: Build NFA directly
            //

            //var start = new Nfa(0, 10);
            //start.AddTrans(0, null, 1);
            //start.AddTrans(0, null, 5);

            //var web = new Nfa(1, 4);
            //web.AddTrans(1, "w", 2);
            //web.AddTrans(2, "e", 3);
            //web.AddTrans(3, "b", 4);

            //var ebay = new Nfa(5, 9);
            //ebay.AddTrans(5, "e", 6);
            //ebay.AddTrans(6, "b", 7);
            //ebay.AddTrans(7, "a", 8);
            //ebay.AddTrans(8, "y", 9);

            //var eps1 = new Nfa(4, 9);
            //eps1.AddTrans(4, null, 9);
            //var eps2 = new Nfa(8, 9);
            //eps2.AddTrans(8, null, 9);

            //// bb
            //Regex reB_Concat_reB = new Seq(reB, reB);
            //// (a|b)*ab
            //Regex r = new Seq(reA_Plus_reB_Star, new Seq(reA, reB));

            //// The regular expression (a|b)*ab
            //BuildAndShow("dfa1.dot", r);
            //// The regular expression ((a|b)*ab)*
            //BuildAndShow("dfa2.dot", new Star(r));
            //// The regular expression ((a|b)*ab)((a|b)*ab)
            //BuildAndShow("dfa3.dot", new Seq(r, r));
            //// The regular expression (a|b)*abb, from ASU 1986 p 136
            //BuildAndShow("dfa4.dot", new Seq(reA_Plus_reB_Star, new Seq(reA, reB_Concat_reB)));

            //// SML reals: sign?((digit+(\.digit+)?))([eE]sign?digit+)?
            //Regex d = new Sym("digit");
            //Regex dPlus = new Seq(d, new Star(d));
            //Regex s = new Sym("sign");
            //Regex sOpt = new Alt(s, new Eps());
            //Regex dot = new Sym(".");
            //Regex dotDigOpt = new Alt(new Eps(), new Seq(dot, dPlus));
            //Regex mant = new Seq(sOpt, new Seq(dPlus, dotDigOpt));
            //Regex e = new Sym("e");
            //Regex exp = new Alt(new Eps(), new Seq(e, new Seq(sOpt, dPlus)));
            //Regex smlReal = new Seq(mant, exp);
            //BuildAndShow("dfa5.dot", smlReal);
        }

        public static void BuildAndShow(string filename, Regex r)
        {
            var path = GetPath(filename);

            // Create epsilon-NFA
            Nfa nfa = r.MkNfa(new Nfa.NameSource());
            Console.WriteLine(nfa);
            Console.WriteLine("---");

            // Create DFA (subset construction)
            Dfa dfa = nfa.ToDfa();
            Console.WriteLine(dfa);
            Console.WriteLine("Writing DFA graph to file " + path);

            // Write DFA to graph
            dfa.WriteDot(path);
            Console.WriteLine();
        }

        private static string GetPath(string filename)
        {
            string path = new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath;
            while (!path.EndsWith("RegExpToDfa"))
            {
                path = Path.GetDirectoryName(path);
            }

            path = Path.Combine(path, filename);
            return path;
        }
    }

}
