using System.Diagnostics.CodeAnalysis;
using AutomataLib;
using ContextFreeGrammar;

namespace GrammarRepo
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static class StanfordReduceReduceConflict
    {
        public enum Sym
        {
            PLUS, // +
            EQUAL, // =
            LPARAN, // (
            RPARAN, // )
            ID, // hardcoded to identifier a in notes
            EOF
        }

        public static Grammar<Sym> GetGrammar()
        {
            // 0: S → E
            // 1: E → E + T
            // 2: E → T
            // 3: E → V = E
            // 4: T → (E)
            // 5: T → ID
            // 6: V → ID
            var grammar = new GrammarBuilder<Sym>()
                .SetNonterminalSymbols(Symbol.Vs("S", "E", "T", "V"))
                //.SetTerminalSymbols(Symbol.Ts('a', '+', '(', ')', '='))
                .SetStartSymbol(Symbol.V("S"))
                .AndProductions(
                    Symbol.V("S").Derives(Symbol.V("E")),
                    Symbol.V("E").Derives(Symbol.V("E"), Symbol.T(Sym.PLUS), Symbol.V("T")),
                    Symbol.V("E").Derives(Symbol.V("T")),
                    // Adding this rule we have a reduce/reduce conflict {reduce 5, reduce 6} in state 5 on every
                    // possible symbol (in LR(0) table), because state 5 contains the following kernel items {T → a•, V → a•}
                    Symbol.V("E").Derives(Symbol.V("V"), Symbol.T(Sym.EQUAL), Symbol.V("E")),
                    Symbol.V("T").Derives(Symbol.T(Sym.LPARAN), Symbol.V("E"), Symbol.T(Sym.RPARAN)),
                    Symbol.V("T").Derives(Symbol.T(Sym.ID)),
                    Symbol.V("V").Derives(Symbol.T(Sym.ID))
                );

            return grammar;
        }
    }
}
