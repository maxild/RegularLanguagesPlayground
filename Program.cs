using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;

namespace RegExpToDfa
{
    class Program
    {
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public static void Main()
        {
            // TestNFA: Trying the RE->NFA->DFA translation on three regular expressions
            //Regex reA = new Sym("a");
            //Regex reB = new Sym("b");

            //// (a|b)*
            //Regex reA_Plus_reB_Star = new Star(new Alt(reA, reB));

            //BuildAndShow("dfa0.dot", reA_Plus_reB_Star);

            //
            // Equivalence of two DFAs (Example 4.21 in book)
            //
            var eqDfas = new Dfa('A', new [] {'A', 'C', 'D'}); // start state is redundant for finding equivalent blocks
            // First DFA
            eqDfas.AddTrans('A', "0", 'A');
            eqDfas.AddTrans('A', "1", 'B');
            eqDfas.AddTrans('B', "0", 'A');
            eqDfas.AddTrans('B', "1", 'B');
            // Second DFA
            eqDfas.AddTrans('C', "0", 'D');
            eqDfas.AddTrans('C', "1", 'E');
            eqDfas.AddTrans('D', "0", 'D');
            eqDfas.AddTrans('D', "1", 'E');
            eqDfas.AddTrans('E', "0", 'C');
            eqDfas.AddTrans('E', "1", 'E');

            eqDfas.SaveDotFile(GetPath("dfa_eq.dot"));

            Console.WriteLine($"Eq state pairs: {eqDfas.DisplayEquivalentPairs()}");
            Console.WriteLine($"Eq state sets: {eqDfas.DisplayMergedEqSets()}");

            //
            // Non-minimal DFA (Exercise 4.4.1 in the book)
            //
            var nonMinDfa = new Dfa('A', new [] {'D'});
            nonMinDfa.AddTrans('A', "0", 'B');
            nonMinDfa.AddTrans('A', "1", 'A');
            nonMinDfa.AddTrans('B', "0", 'A');
            nonMinDfa.AddTrans('B', "1", 'C');
            nonMinDfa.AddTrans('C', "0", 'D');
            nonMinDfa.AddTrans('C', "1", 'B');
            nonMinDfa.AddTrans('D', "0", 'D');
            nonMinDfa.AddTrans('D', "1", 'A');
            nonMinDfa.AddTrans('E', "0", 'D');
            nonMinDfa.AddTrans('E', "1", 'F');
            nonMinDfa.AddTrans('F', "0", 'G');
            nonMinDfa.AddTrans('F', "1", 'E');
            nonMinDfa.AddTrans('G', "0", 'F');
            nonMinDfa.AddTrans('G', "1", 'G');
            nonMinDfa.AddTrans('H', "0", 'G');
            nonMinDfa.AddTrans('H', "1", 'D');

            nonMinDfa.SaveDotFile(GetPath("dfaNonMin.dot"));

            Console.WriteLine($"Eq state pairs: {nonMinDfa.DisplayEquivalentPairs()}");
            Console.WriteLine($"Eq state sets: {nonMinDfa.DisplayMergedEqSets()}");

            Dfa minDfa = nonMinDfa.ToMinimumDfa();

            minDfa.SaveDotFile(GetPath("dfaMin.dot"));

            //
            // epsilon-NFA accepting accepting decimal numbers
            //

            var nfaDecimal = new Nfa(0, 5);

            // TODO: Because we do not support ranges let d = [0-9]
            // TODO: Support characterRanges as spacial labels/inputs on transitions
            // TODO: Support putting single arc on every transition from p to q where label uses Sigma \ chars notation
            //            Sigma - {...}
            //            Sigma - d
            //            { .... }
            // TODO: Have the program calculate the label with fewest characters, and always use single arcs between any two nodes

            // sign
            nfaDecimal.AddTrans(0, null, 1);
            nfaDecimal.AddTrans(0, "+", 1);
            nfaDecimal.AddTrans(0, "-", 1);
            // optional digits [0-9] before decimal point
            nfaDecimal.AddTrans(1, "d", 1);
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
            nfaDecimal.AddTrans(1, ".", 2);
            // digit after state 2
            nfaDecimal.AddTrans(2, "d", 3);
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
            nfaDecimal.AddTrans(1, "d", 4);
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
            nfaDecimal.AddTrans(4, ".", 3);
            // optional digits [0-9] after decimal point
            nfaDecimal.AddTrans(3, "d", 3);
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
            nfaDecimal.AddTrans(3, null, 5);

            Dfa dfaDecimal = nfaDecimal.ToDfa();

            dfaDecimal.SaveDotFile(GetPath("dfa_decimal.dot"));

            foreach (var word in new [] {"+d.d", "-.", "-.d", ".", "d.", "d.d", ".d"})
            {
                Console.WriteLine($"dfaDecimal.Match({word}) = {dfaDecimal.Match(word)}");
            }

            //
            // Keyword search: Build NFA directly
            //

            // TODO: Vi antager, at alfabetet er de mulige ord i 'web' og 'ebay', da grafen ellers bliver meget uoverskuelig
            // NOTE: Grafen er allerede uoverskuelig pga de mange pile, da hver vertex kun kan have et input

            // 9,1,0 is part of every state
            var nfaKeywords = new Nfa(9, new [] {4, 8}, s => new Set<int>(new[] {0,1,9}).Contains(s) == false);
            nfaKeywords.AddTrans(9, null, 1);
            nfaKeywords.AddTrans(9, null, 0);
            // guessing is smart in NFA
            nfaKeywords.AddTrans(9, "w", 9);
            nfaKeywords.AddTrans(9, "e", 9);
            nfaKeywords.AddTrans(9, "b", 9);
            nfaKeywords.AddTrans(9, "a", 9);
            nfaKeywords.AddTrans(9, "y", 9);
            // web
            nfaKeywords.AddTrans(1, "w", 2);
            nfaKeywords.AddTrans(2, "e", 3);
            nfaKeywords.AddTrans(3, "b", 4);
            // ebay
            nfaKeywords.AddTrans(0, "e", 5);
            nfaKeywords.AddTrans(5, "b", 6);
            nfaKeywords.AddTrans(6, "a", 7);
            nfaKeywords.AddTrans(7, "y", 8);

            Dfa dfaKeywords = nfaKeywords.ToDfa();

            // Den virker, men grafen er uoverskuelig da vi ikke kan placere noderne
            dfaKeywords.SaveDotFile(GetPath("dfa_keywords.dot"));

            Console.WriteLine("");
            foreach (var word in new [] {"goto", "web", "ebay", "webay", "web1"})
            {
                // NFA is tail whatever, that is webay is a match because the suffix ebay is matched
                Console.WriteLine($"dfaKeywords.Match({word}) = {dfaKeywords.Match(word)}");
            }


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
            dfa.SaveDotFile(path);
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
