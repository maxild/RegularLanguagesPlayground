using System.Diagnostics.CodeAnalysis;
using ContextFreeGrammar;

namespace GrammarRepo
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static class StanfordExprGrammar
    {
        public enum Sym
        {
            EOF,
            PLUS,       // +
            LPARAN,     // (
            RPARAN,     // )
            ID         // a
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
            // 4: T → a
            var grammar = new GrammarBuilder()
                .Terminals<Sym>()
                .Nonterminals<Var>()
                .StartSymbol(Var.S)
                .And(g => g.Rules(
                        g[Var.S].Derives(g[Var.E]),
                        g[Var.E].Derives(g[Var.E], g[Sym.PLUS], g[Var.T]),
                        g[Var.E].Derives(g[Var.T]),
                        g[Var.T].Derives(g[Sym.LPARAN], g[Var.E], g[Sym.RPARAN]),
                        g[Var.T].Derives(g[Sym.ID])
                    )
                );

            return grammar;
        }
    }
}
