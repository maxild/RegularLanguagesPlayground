using System.Diagnostics.CodeAnalysis;
using ContextFreeGrammar;
using ContextFreeGrammar.Analyzers;

namespace GrammarRepo
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static class DragonBookExample4_48
    {
        public enum Sym
        {
            EOF,
            EQUAL,    // =
            ID,       // ID
            ASTERISK  // *
        }

        public enum Var
        {
            Start,
            S,
            L,
            R
        }

        /// <summary>
        /// Example 4.48 in Dragon Book.
        /// G3 in "A Survey of LR-Parsing Methods", Gallier.
        /// </summary>
        public static Grammar<Sym, Var> GetGrammar()
        {
            // NOTE: We are using nonterminal L for l-value (a location), nonterminal R for r-value (value
            //       that can be stored in a location), and terminal * for 'content-of' prefix operator.
            // 0: S' → S
            // 1: S → L = R
            // 2: S → R
            // 3: L → *R
            // 4: L → ID
            // 5: R → L
            var grammar = new GrammarBuilder()
                .Terminals<Sym>()
                .Nonterminals<Var>()
                .SetAnalyzer(Analyzers.CreateDigraphAlgorithmAnalyzer)
                .StartSymbol(Var.Start)
                .And(g => g.Rules(
                        g[Var.Start].Derives(g[Var.S]),
                        g[Var.S].Derives(g[Var.L], g[Sym.EQUAL], g[Var.R]),
                        g[Var.S].Derives(g[Var.R]),
                        g[Var.L].Derives(g[Sym.ASTERISK], g[Var.R]),
                        g[Var.L].Derives(g[Sym.ID]),
                        g[Var.R].Derives(g[Var.L])
                    )
                );

            return grammar;
        }

        public static Grammar<Sym, Var> GetExtendedGrammar()
        {
            // NOTE: We are using nonterminal L for l-value (a location), nonterminal R for r-value (value
            //       that can be stored in a location), and terminal * for 'content-of' prefix operator.
            // 0: S' → S$
            // 1: S  → L = R
            // 2: S  → R
            // 3: R  → *R
            // 4: R  → ID
            // 5: R  → L
            var grammar = new GrammarBuilder()
                .Terminals<Sym>()
                .Nonterminals<Var>()
                .SetAnalyzer(Analyzers.CreateDigraphAlgorithmAnalyzer)
                .StartSymbol(Var.Start)
                .And(g => g.Rules(
                        g[Var.Start].Derives(g[Var.S], g[Sym.EOF]),
                        g[Var.S].Derives(g[Var.L], g[Sym.EQUAL], g[Var.R]),
                        g[Var.S].Derives(g[Var.R]),
                        g[Var.L].Derives(g[Sym.ASTERISK], g[Var.R]),
                        g[Var.L].Derives(g[Sym.ID]),
                        g[Var.R].Derives(g[Var.L])
                    )
                );

            return grammar;

        }
    }
}
