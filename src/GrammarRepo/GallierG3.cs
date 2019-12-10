using AutomataLib;
using ContextFreeGrammar;

namespace GrammarRepo
{
    public static class GallierG3
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
            // 0: S → E
            // 1: E → aEb
            // 2: E → ε
            var grammar = new GrammarBuilder<Sym>()
                .SetNonterminalSymbols(Symbol.Vs("S", "E"))
                .SetStartSymbol(Symbol.V("S"))
                .AndProductions(
                    Symbol.V("S").Derives(Symbol.V("E")),
                    Symbol.V("E").Derives(Symbol.T(Sym.a), Symbol.V("E"), Symbol.T(Sym.b)),
                    Symbol.V("E").Derives(Symbol.Epsilon)
                );

            return grammar;
        }
    }
}
