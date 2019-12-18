using System.Diagnostics.CodeAnalysis;
using ContextFreeGrammar;

namespace GrammarRepo
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static class GallierG1
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
            // Augmented Grammar (assumed reduced, i.e. no useless symbols).
            //
            // ({S,E}, {a,b}, P, S) with P given by
            //
            // The purpose of this new starting production (S) is to indicate to the parser when
            // it should stop parsing and announce acceptance of input.
            //
            // 0: S → E
            // 1: E → aEb
            // 2: E → ab
            var grammar = new GrammarBuilder()
                .Terminals<Sym>()
                .Nonterminals<Var>()
                .StartSymbol(Var.S)
                .And(g => g.Rules(
                        g[Var.S].Derives(g[Var.E]),
                        g[Var.E].Derives(g[Sym.a], g[Var.E], g[Sym.b]),
                        g[Var.E].Derives(g[Sym.a], g[Sym.b])
                    )
                );

            return grammar;
        }
    }
}
