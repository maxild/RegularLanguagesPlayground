using AutomataLib;
using ContextFreeGrammar;

namespace GrammarRepo
{
    public static class GallierG1
    {
        public enum Sym
        {
            EPS,
            a,
            b,
            EOF
        }

        public static Grammar<Sym> GetGrammar()
        {
            // Augmented Grammar (assumed reduced, i.e. no useless symbols).
            //
            // ({S,E}, {a,b}, P, S) with P given by
            //
            // The purpose of this new starting production (S) is to indicate to the parser when
            // it should stop parsing and announce acceptance of input.
            //
            // 0: S → E
            // 1: E → aEb
            // 2: E → ab
            var grammar = new GrammarBuilder<Sym>()
                .SetNonterminalSymbols(Symbol.Vs("S", "E"))
                .SetStartSymbol(Symbol.V("S"))
                .AndProductions(
                    Symbol.V("S").Derives(Symbol.V("E")),
                    Symbol.V("E").Derives(Symbol.T(Sym.a), Symbol.V("E"), Symbol.T(Sym.b)),
                    Symbol.V("E").Derives(Symbol.T(Sym.a), Symbol.T(Sym.b))
                );

            return grammar;
        }
    }
}
