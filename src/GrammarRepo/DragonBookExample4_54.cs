using System.Diagnostics.CodeAnalysis;
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
            EOF,
            c,
            d
        }

        public enum Var
        {
            Start,
            S,
            C
        }

        public static Grammar<Sym, Var> GetGrammar()
        {
            // Regular Language for c*dc*d
            // 0: S' → S
            // 1: S → CC
            // 2: C → cC
            // 3: C → d
            var grammar = new GrammarBuilder()
                .Terminals<Sym>()
                .Nonterminals<Var>()
                .StartSymbol(Var.Start)
                .And(g => g.Rules(
                        g[Var.Start].Derives(g[Var.S]),
                        g[Var.S].Derives(g[Var.C], g[Var.C]),
                        g[Var.C].Derives(g[Sym.c], g[Var.C]),
                        g[Var.C].Derives(g[Sym.d])
                    )
                );

            return grammar;
        }
    }
}
