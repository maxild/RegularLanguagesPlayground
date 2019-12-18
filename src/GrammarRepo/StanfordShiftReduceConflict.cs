using System.Diagnostics.CodeAnalysis;
using ContextFreeGrammar;

namespace GrammarRepo
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static class StanfordShiftReduceConflict
    {
        public enum Sym
        {
            EOF,
            PLUS,       // +
            LPARAN,     // (
            RPARAN,     // )
            LBRACKET,   // [
            RBRACKET,   // ]
            ID          // hardcoded to identifier a in notes
        }

        public enum Var
        {
            S,
            E,
            T
        }

        public static Grammar<Sym, Var> GetGrammar()
        {
            // 0: S → E
            // 1: E → E + T
            // 2: E → T
            // 3: T → (E)
            // 4: T → ID
            // 5: T → ID[E]
            var grammar = new GrammarBuilder()
                .Terminals<Sym>()
                .Nonterminals<Var>()
                .StartSymbol(Var.S)
                .And(g => g.Rules(
                        g[Var.S].Derives(g[Var.E]),
                        g[Var.E].Derives(g[Var.E], g[Sym.PLUS], g[Var.T]),
                        g[Var.E].Derives(g[Var.T]),
                        g[Var.T].Derives(g[Sym.LPARAN], g[Var.E], g[Sym.RPARAN]),
                        g[Var.T].Derives(g[Sym.ID]),
                        // Adding this rule we have a shift/reduce conflict {shift 7, reduce 4} on '[' in state 4,
                        // because state 4 contains the following kernel items {T → a•, T → a•[E]}
                        g[Var.T].Derives(g[Sym.ID], g[Sym.LBRACKET], g[Var.E], g[Sym.RBRACKET])
                    )
                );

            return grammar;
        }
    }
}
