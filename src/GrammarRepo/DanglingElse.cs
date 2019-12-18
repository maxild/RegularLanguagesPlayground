using System.Diagnostics.CodeAnalysis;
using ContextFreeGrammar;

namespace GrammarRepo
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static class DanglingElse
    {
        public enum Sym
        {
            EOF,
            IF,
            THEN,
            ELSE,
            TRUE,
            FALSE
        }

        public enum Var
        {
            S,        // S'
            Stmt,     // S
            Expr      // E
        }

        public static Grammar<Sym, Var> GetGrammar()
        {
            // 0: S' → S$
            // 1: S → i E t S
            // 2: S → i E t S e S
            // 3: E → 0
            // 4: E → 1
            // where tokens i (if), t (then), e (else)
            var grammar = new GrammarBuilder()
                .Terminals<Sym>()
                .Nonterminals<Var>()
                .StartSymbol(Var.S)
                .And(g => g.Rules(
                        g[Var.S].Derives(g[Var.Stmt]),
                        g[Var.Stmt].Derives(g[Sym.IF], g[Var.Expr], g[Sym.THEN], g[Var.Stmt]),
                        g[Var.Stmt].Derives(g[Sym.IF], g[Var.Expr], g[Sym.THEN], g[Var.Stmt], g[Sym.ELSE], g[Var.Stmt]),
                        g[Var.Expr].Derives(g[Sym.TRUE]),
                        g[Var.Expr].Derives(g[Sym.FALSE])
                    )
                );

            return grammar;
        }
    }
}
