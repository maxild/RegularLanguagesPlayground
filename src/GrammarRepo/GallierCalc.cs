using System;
using AutomataLib;
using ContextFreeGrammar;

namespace GrammarRepo
{
    public static class GallierCalc
    {
        public enum Sym
        {
            EPS = 0,
            PLUS,       // +
            MINUS,      // -
            ASTERISK,   // *
            LPARAN,     // (
            RPARAN,     // )
            ID,         // hardcoded to identifier a in notes
            EOF
        }

        public static Grammar<Sym> GetGrammar()
        {
            // G1 from p. 12 on https://www.cis.upenn.edu/~jean/gbooks/graphm.pdf
            // 0: S → E$
            // 1: E → E+T
            // 2: E → T
            // 3: T → T*F
            // 4: T → F
            // 5: F → (E)
            // 6: F → -T
            // 7: F → ID
            var grammar = new GrammarBuilder<Sym>()
                .SetNonterminalSymbols(Symbol.Vs("S", "E", "T", "F"))
                .SetStartSymbol(Symbol.V("S"))
                .AndProductions(
                    Symbol.V("S").Derives(Symbol.V("E"), Symbol.Eof<Sym>()),
                    Symbol.V("E").Derives(Symbol.V("E"), Symbol.T(Sym.PLUS), Symbol.V("T")),
                    Symbol.V("E").Derives(Symbol.V("T")),
                    Symbol.V("T").Derives(Symbol.V("T"), Symbol.T(Sym.ASTERISK), Symbol.V("F")),
                    Symbol.V("T").Derives(Symbol.V("F")),
                    Symbol.V("F").Derives(Symbol.T(Sym.LPARAN), Symbol.V("E"), Symbol.T(Sym.RPARAN)),
                    Symbol.V("F").Derives(Symbol.T(Sym.MINUS), Symbol.V("T")),
                    Symbol.V("F").Derives(Symbol.T(Sym.ID))
                );

            return grammar;
        }
    }
}
