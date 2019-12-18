using System.Diagnostics.CodeAnalysis;
using ContextFreeGrammar;

namespace GrammarRepo
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static class GallierG3
    {
        public enum Sym
        {
            EOF,
            a,
            b
        }

        public enum Var
        {
            S,
            E
        }

        public static Grammar<Sym, Var> GetGrammar()
        {
            // 0: S → E
            // 1: E → aEb
            // 2: E → ε
            var grammar = new GrammarBuilder()
                .Terminals<Sym>()
                .Nonterminals<Var>()
                .StartSymbol(Var.S)
                .And(g => g.Rules(
                        g[Var.S].Derives(g[Var.E]),
                        g[Var.E].Derives(g[Sym.a], g[Var.E], g[Sym.b]),
                        g[Var.E].DerivesEpsilon()
                    )
                );

            return grammar;
        }
    }
}
