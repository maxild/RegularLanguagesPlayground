using System.Diagnostics.CodeAnalysis;
using ContextFreeGrammar;

namespace GrammarRepo
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static class GallierCalc
    {
        public enum Sym
        {
            EOF,
            PLUS,       // +
            MINUS,      // -
            ASTERISK,   // *
            LPARAN,     // (
            RPARAN,     // )
            ID          // hardcoded to identifier a in notes
        }

        public enum Var
        {
            S,
            E,
            T,
            F
        }

        public static Grammar<Sym, Var> GetGrammar()
        {
            // G1 from p. 12 on https://www.cis.upenn.edu/~jean/gbooks/graphm.pdf
            // 0: S → E$
            // 1: E → E+T
            // 2: E → T
            // 3: T → T*F
            // 4: T → F
            // 5: F → (E)
            // 6: F → -T
            // 7: F → ID
            var grammar = new GrammarBuilder()
                .Terminals<Sym>()
                .Nonterminals<Var>()
                .StartSymbol(Var.S)
                .And(g => g.Rules(
                        g[Var.S].Derives(g[Var.E], g[Sym.EOF]),
                        g[Var.E].Derives(g[Var.E], g[Sym.PLUS], g[Var.T]),
                        g[Var.E].Derives(g[Var.T]),
                        g[Var.T].Derives(g[Var.T], g[Sym.ASTERISK], g[Var.F]),
                        g[Var.T].Derives(g[Var.F]),
                        g[Var.F].Derives(g[Sym.LPARAN], g[Var.E], g[Sym.RPARAN]),
                        g[Var.F].Derives(g[Sym.MINUS], g[Var.T]),
                        g[Var.F].Derives(g[Sym.ID])
                    )
                );

            return grammar;
        }
    }
}
