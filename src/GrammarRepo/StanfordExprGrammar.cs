using System.Diagnostics.CodeAnalysis;
using AutomataLib;
using ContextFreeGrammar;

namespace GrammarRepo
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static class StanfordExprGrammar
    {
        public enum Sym
        {
            PLUS,       // +
            LPARAN,     // (
            RPARAN,     // )
            ID,         // a
            EOF
        }

        public static Grammar<Sym> GetGrammar()
        {
            // 0: S → E
            // 1: E → E + T
            // 2: E → T
            // 3: T → (E)
            // 4: T → a
            var grammar = new GrammarBuilder<Sym>()
                .SetNonterminalSymbols(Symbol.Vs("S", "E", "T"))
                .SetStartSymbol(Symbol.V("S"))
                .AndProductions(
                    Symbol.V("S").Derives(Symbol.V("E")),
                    Symbol.V("E").Derives(Symbol.V("E"), Symbol.T(Sym.PLUS), Symbol.V("T")),
                    Symbol.V("E").Derives(Symbol.V("T")),
                    Symbol.V("T").Derives(Symbol.T(Sym.LPARAN), Symbol.V("E"), Symbol.T(Sym.RPARAN)),
                    Symbol.V("T").Derives(Symbol.T(Sym.ID))
                );
            return grammar;
        }
    }
}
