using System.Diagnostics.CodeAnalysis;
using AutomataLib;
using ContextFreeGrammar;

namespace GrammarRepo
{
    /// <summary>
    /// Dragon book example 4.54, p. 263, 2nd ed.
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static class DragonBookExample4_54
    {
        public enum Sym
        {
            c,      // c
            d,      // d
            EOF
        }

        public static Grammar<Sym> GetGrammar()
        {
            // Regular Language for c*dc*d
            // 0: S' → S
            // 1: S → CC
            // 2: C → cC
            // 3: C → d
            var grammar = new GrammarBuilder<Sym>()
                .SetNonterminalSymbols(Symbol.Vs("S'", "S", "C"))
                .SetStartSymbol(Symbol.V("S'"))
                .AndProductions(
                    Symbol.V("S'").Derives(Symbol.V("S")),
                    Symbol.V("S").Derives(Symbol.V("C"), Symbol.V("C")),
                    Symbol.V("C").Derives(Symbol.T(Sym.c), Symbol.V("C")),
                    Symbol.V("C").Derives(Symbol.T(Sym.d))
                );

            return grammar;
        }
    }
}
