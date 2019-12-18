using System.Diagnostics.CodeAnalysis;
using ContextFreeGrammar;
using Shouldly;
using Xunit;

namespace UnitTests
{
    public class GrammarBuilderTests
    {
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        enum Tok
        {
            EOF,
            ID,
            NUM,
            PLUS,
            MINUS
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        enum Var
        {
            S,
            Expr,
            Factor,
            Term
        }

        [Fact]
        public void Test()
        {
            var grammar = new GrammarBuilder()
                .Terminals<Tok>()
                .Nonterminals<Var>()
                .StartSymbol(Var.S)
                .And(g => g.Rules(
                        g[Var.Expr].Derives(g[Var.Expr], g[Tok.PLUS], g[Var.Expr]),
                        g[Var.Expr].Derives(g[Var.Expr], g[Tok.MINUS], g[Var.Expr])
                    )
                );

            grammar.ShouldNotBeNull();
        }
    }
}
