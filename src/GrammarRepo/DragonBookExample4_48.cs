using System.Diagnostics.CodeAnalysis;
using AutomataLib;
using ContextFreeGrammar;
using ContextFreeGrammar.Analyzers;

namespace GrammarRepo
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static class DragonBookExample4_48
    {
        public enum Sym
        {
            EPS = 0,
            EQUAL,    // =
            ID,       // ID
            ASTERISK, // *
            EOF
        }

        /// <summary>
        /// Example 4.48 in Dragon Book.
        /// G3 in "A Survey of LR-Parsing Methods", Gallier.
        /// </summary>
        public static Grammar<Sym> GetGrammar()
        {
            // NOTE: We are using nonterminal L for l-value (a location), nonterminal R for r-value (value
            //       that can be stored in a location), and terminal * for 'content-of' prefix operator.
            // 0: S' → S
            // 1: S → L = R
            // 2: S → R
            // 3: L → *R
            // 4: L → ID
            // 5: R → L
            var grammar = new GrammarBuilder<Sym>()
                .SetAnalyzer(Analyzers.CreateDigraphAlgorithmAnalyzer)
                .SetNonterminalSymbols(Symbol.Vs("S'", "S", "R", "L"))
                .SetStartSymbol(Symbol.V("S'"))
                .AndProductions(
                    Symbol.V("S'").Derives(Symbol.V("S")),
                    Symbol.V("S").Derives(Symbol.V("L"), Symbol.T(Sym.EQUAL), Symbol.V("R")),
                    Symbol.V("S").Derives(Symbol.V("R")),
                    Symbol.V("L").Derives(Symbol.T(Sym.ASTERISK), Symbol.V("R")),
                    Symbol.V("L").Derives(Symbol.T(Sym.ID)),
                    Symbol.V("R").Derives(Symbol.V("L"))
                );

            return grammar;
        }

        public static Grammar<Sym> GetExtendedGrammar()
        {
            // NOTE: We are using nonterminal L for l-value (a location), nonterminal R for r-value (value
            //       that can be stored in a location), and terminal * for 'content-of' prefix operator.
            // 0: S' → S$
            // 1: S  → L = R
            // 2: S  → R
            // 3: R  → *R
            // 4: R  → ID
            // 5: R  → L
            var grammar = new GrammarBuilder<Sym>()
                .SetAnalyzer(Analyzers.CreateDigraphAlgorithmAnalyzer)
                .SetNonterminalSymbols(Symbol.Vs("S'", "S", "R", "L"))
                .SetStartSymbol(Symbol.V("S'"))
                .AndProductions(
                    Symbol.V("S'").Derives(Symbol.V("S"), Symbol.Eof<Sym>()),
                    Symbol.V("S").Derives(Symbol.V("L"), Symbol.T(Sym.EQUAL), Symbol.V("R")),
                    Symbol.V("S").Derives(Symbol.V("R")),
                    Symbol.V("L").Derives(Symbol.T(Sym.ASTERISK), Symbol.V("R")),
                    Symbol.V("L").Derives(Symbol.T(Sym.ID)),
                    Symbol.V("R").Derives(Symbol.V("L"))
                );

            return grammar;

        }
    }
}
