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
            PLUS,       // +
            ASTERISK,   // *
            LPARAN,     // (
            RPARAN,     // )
            ID,
            EOF
        }

        public static Grammar<Sym> GetGrammar()
        {
            // NOTE: Augmented, but without explicit EOF symbol (the EOF marker is optional, but changes the number of states)
            // 0: S → E
            // 1: E → E + T
            // 2: E → T
            // 3: T → T * F
            // 4: T → F
            // 5: T → (E)
            // 6: T → ID
            var grammar = new GrammarBuilder<Sym>()
                .SetNonterminalSymbols(Symbol.Vs("S", "E", "T", "F"))
                .SetStartSymbol(Symbol.V("S"))
                .AndProductions(
                    Symbol.V("S").Derives(Symbol.V("E")),
                    Symbol.V("E").Derives(Symbol.V("E"), Symbol.T(Sym.PLUS), Symbol.V("T")),
                    Symbol.V("E").Derives(Symbol.V("T")),
                    Symbol.V("T").Derives(Symbol.V("T"), Symbol.T(Sym.ASTERISK), Symbol.V("F")),
                    Symbol.V("T").Derives(Symbol.V("F")),
                    Symbol.V("F").Derives(Symbol.T(Sym.LPARAN), Symbol.V("E"), Symbol.T(Sym.RPARAN)),
                    Symbol.V("F").Derives(Symbol.T(Sym.ID))
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
