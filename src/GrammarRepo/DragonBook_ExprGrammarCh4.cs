using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using AutomataLib;
using ContextFreeGrammar;
using ContextFreeGrammar.Lexers;

namespace GrammarRepo
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static class DragonBook_ExprGrammarCh4
    {
        public enum Sym
        {
            EOF,
            PLUS,       // +
            ASTERISK,   // *
            LPARAN,     // (
            RPARAN,     // )
            ID
        }

        public enum Var
        {
            S,
            E,
            T,
            F,
        }

        public static Grammar<Sym, Var> GetGrammar()
        {
            // NOTE: Augmented, but without explicit EOF symbol (the EOF marker is optional, but changes the number of states)
            // 0: S → E
            // 1: E → E + T
            // 2: E → T
            // 3: T → T * F
            // 4: T → F
            // 5: F → (E)
            // 6: F → ID
            var grammar = new GrammarBuilder()
                .Terminals<Sym>()
                .Nonterminals<Var>()
                .StartSymbol(Var.S)
                .And(g => g.Rules(
                        g[Var.S].Derives(g[Var.E]),
                        g[Var.E].Derives(g[Var.E], g[Sym.PLUS], g[Var.T]),
                        g[Var.E].Derives(g[Var.T]),
                        g[Var.T].Derives(g[Var.T], g[Sym.ASTERISK], g[Var.F]),
                        g[Var.T].Derives(g[Var.F]),
                        g[Var.F].Derives(g[Sym.LPARAN], g[Var.E], g[Sym.RPARAN]),
                        g[Var.F].Derives(g[Sym.ID])
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
                    case '(':
                        tokens.Add((Sym.LPARAN, "("));
                        break;
                    case ')':
                        tokens.Add((Sym.RPARAN, ")"));
                        break;
                    case '+':
                        tokens.Add((Sym.PLUS, "+"));
                        break;
                    case '*':
                        tokens.Add((Sym.ASTERISK, "*"));
                        break;
                    default:
                        tokens.Add((Sym.ID, new string(letter, 1)));
                        break;
                }
            }

            return new FakeLexer<Sym>(tokens);
        }
    }
}
