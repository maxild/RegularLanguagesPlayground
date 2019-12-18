using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using AutomataLib;
using ContextFreeGrammar;
using ContextFreeGrammar.Lexers;

namespace GrammarRepo
{
    /// <summary>
    /// Dragon book example 4.52, p. 261, 2nd ed.
    /// (identical to example 4.54, p. 263)
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static class DragonBookExample4_52
    {
        public enum Sym
        {
            EOF,
            a,
            b
        }

        public enum Var
        {
            Start,
            S,
            B
        }

        public static Grammar<Sym, Var> GetGrammar()
        {
            // Regular language for a*ba*b
            // 0: S' → S
            // 1: S → BB
            // 2: B → aB
            // 3: B → b
            var grammar = new GrammarBuilder()
                .Terminals<Sym>()
                .Nonterminals<Var>()
                .StartSymbol(Var.Start)
                .And(g => g.Rules(
                        g[Var.Start].Derives(g[Var.S]),
                        g[Var.S].Derives(g[Var.B], g[Var.B]),
                        g[Var.B].Derives(g[Sym.a], g[Var.B]),
                        g[Var.B].Derives(g[Sym.b])
                    )
                );

            return grammar;
        }

        public static IBufferedLexer<Token<Sym>> GetLexer(string input)
        {
            List<(Sym, string)> tokens = new List<(Sym, string)>();
            foreach (char letter in Letterizer<char>.Default.GetLetters(input))
            {
                switch (letter)
                {
                    case 'a':
                        tokens.Add((Sym.a, "a"));
                        break;
                    case 'b':
                        tokens.Add((Sym.b, "b"));
                        break;
                    default:
                        throw new ArgumentException($"The character '{letter}' is not supported by the test grammar.");
                }
            }

            return new FakeLexer<Sym>(tokens);
        }
    }
}
