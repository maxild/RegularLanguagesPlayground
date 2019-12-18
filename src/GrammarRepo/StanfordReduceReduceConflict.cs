using System.Diagnostics.CodeAnalysis;
using ContextFreeGrammar;

namespace GrammarRepo
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static class StanfordReduceReduceConflict
    {
        public enum Sym
        {
            EOF,
            PLUS, // +
            EQUAL, // =
            LPARAN, // (
            RPARAN, // )
            ID     // hardcoded to identifier a in notes
        }

        public enum Var
        {
            S,
            E,
            T,
            V
        }

        public static Grammar<Sym, Var> GetGrammar()
        {
            // 0: S → E
            // 1: E → E + T
            // 2: E → T
            // 3: E → V = E
            // 4: T → (E)
            // 5: T → ID
            // 6: V → ID
            var grammar = new GrammarBuilder()
                .Terminals<Sym>()
                .Nonterminals<Var>()
                .StartSymbol(Var.S)
                .And(g => g.Rules(
                        g[Var.S].Derives(g[Var.E]),
                        g[Var.E].Derives(g[Var.E], g[Sym.PLUS], g[Var.T]),
                        g[Var.E].Derives(g[Var.T]),
                        // Adding this rule we have a reduce/reduce conflict {reduce 5, reduce 6} in state 5 on every
                        // possible symbol (in LR(0) table), because state 5 contains the following kernel items {T → a•, V → a•}
                        g[Var.E].Derives(g[Var.V], g[Sym.EQUAL], g[Var.E]),
                        g[Var.T].Derives(g[Sym.LPARAN], g[Var.E], g[Sym.RPARAN]),
                        g[Var.T].Derives(g[Sym.ID]),
                        g[Var.V].Derives(g[Sym.ID])
                    )
                );

            return grammar;
        }
    }
}
