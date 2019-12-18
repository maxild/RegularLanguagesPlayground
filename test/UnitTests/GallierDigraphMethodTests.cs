using System.Linq;
using ContextFreeGrammar;
using GrammarRepo;
using Shouldly;
using Xunit;
using Sym = GrammarRepo.GallierCalc.Sym;
using Var = GrammarRepo.GallierCalc.Var;

namespace UnitTests
{
    public class GallierDigraphMethodTests
    {
        public GallierDigraphMethodTests()
        {
            Grammar = GallierCalc.GetGrammar();
        }

        private Grammar<Sym, Var> Grammar { get; }

        [Fact]
        public void ErasableEdgeCases()
        {
            Grammar.Erasable(Symbol.Epsilon).ShouldBeTrue();
            Grammar.Erasable(Grammar.Eof()).ShouldBeTrue();
        }

        [Fact]
        public void Erasable()
        {
            // No ε-productions, no nullable/erasable symbols
            Grammar.Nonterminals.Each(symbol => Grammar.Erasable(symbol).ShouldBeFalse());
            Enumerable.Range(0, Grammar.Productions.Count).Each(i => Grammar.Erasable(i).ShouldBeFalse());

            Grammar.Erasable(Grammar.V(Var.S)).ShouldBeFalse();
            Grammar.Erasable(Grammar.V(Var.E)).ShouldBeFalse();
            Grammar.Erasable(Grammar.V(Var.T)).ShouldBeFalse();
            Grammar.Erasable(Grammar.V(Var.F)).ShouldBeFalse();
        }

        [Fact]
        public void FirstEdgeCases()
        {
            Grammar.First(Symbol.Epsilon).ShouldBeEmpty();
            Grammar.First(Grammar.Eof()).ShouldSetEqual(Grammar.Eof());
        }

        [Fact]
        public void First()
        {
            Grammar.First(Grammar.V(Var.S)).ShouldSetEqual(Grammar.Ts(Sym.LPARAN, Sym.MINUS, Sym.ID));
            Grammar.First(Grammar.V(Var.E)).ShouldSetEqual(Grammar.Ts(Sym.LPARAN, Sym.MINUS, Sym.ID));
            Grammar.First(Grammar.V(Var.T)).ShouldSetEqual(Grammar.Ts(Sym.LPARAN, Sym.MINUS, Sym.ID));
            Grammar.First(Grammar.V(Var.F)).ShouldSetEqual(Grammar.Ts(Sym.LPARAN, Sym.MINUS, Sym.ID));

            Grammar.First(0).ShouldSetEqual(Grammar.Ts(Sym.LPARAN, Sym.MINUS, Sym.ID));
            Grammar.First(1).ShouldSetEqual(Grammar.Ts(Sym.LPARAN, Sym.MINUS, Sym.ID));
            Grammar.First(2).ShouldSetEqual(Grammar.Ts(Sym.LPARAN, Sym.MINUS, Sym.ID));
            Grammar.First(3).ShouldSetEqual(Grammar.Ts(Sym.LPARAN, Sym.MINUS, Sym.ID));
            Grammar.First(4).ShouldSetEqual(Grammar.Ts(Sym.LPARAN, Sym.MINUS, Sym.ID));
            Grammar.First(5).ShouldSetEqual(Grammar.T(Sym.LPARAN));
            Grammar.First(6).ShouldSetEqual(Grammar.T(Sym.MINUS));
            Grammar.First(7).ShouldSetEqual(Grammar.T(Sym.ID));
        }

        [Fact]
        public void Follow()
        {
            Grammar.Follow(Grammar.V(Var.S)).ShouldBeEmpty();
            Grammar.Follow(Grammar.V(Var.E)).ShouldSetEqual(Grammar.Ts(Sym.PLUS, Sym.RPARAN, Sym.EOF));
            Grammar.Follow(Grammar.V(Var.T)).ShouldSetEqual(Grammar.Ts(Sym.PLUS, Sym.ASTERISK, Sym.RPARAN, Sym.EOF));
            Grammar.Follow(Grammar.V(Var.F)).ShouldSetEqual(Grammar.Ts(Sym.PLUS, Sym.ASTERISK, Sym.RPARAN, Sym.EOF));
        }
    }
}
