using System.Linq;
using AutomataLib;
using ContextFreeGrammar;
using GrammarRepo;
using Shouldly;
using Xunit;
using Sym = GrammarRepo.GallierCalc.Sym;

namespace UnitTests
{
    public class DragonBookAnalyzerTests
    {
        public DragonBookAnalyzerTests()
        {
            Grammar = GallierCalc.GetGrammar();
        }

        private Grammar<Sym> Grammar { get; }

        [Fact]
        public void ErasableEdgeCases()
        {
            Grammar.Erasable(Symbol.Epsilon).ShouldBeTrue();
            Grammar.Erasable(Symbol.Eof<Sym>()).ShouldBeTrue();
        }

        [Fact]
        public void Erasable()
        {
            // No ε-productions, no nullable/erasable symbols
            Grammar.Variables.Each(symbol => Grammar.Erasable(symbol).ShouldBeFalse());
            Enumerable.Range(0, Grammar.Productions.Count).Each(i => Grammar.Erasable(i).ShouldBeFalse());

            Grammar.Erasable(Symbol.V("S")).ShouldBeFalse();
            Grammar.Erasable(Symbol.V("E")).ShouldBeFalse();
            Grammar.Erasable(Symbol.V("T")).ShouldBeFalse();
            Grammar.Erasable(Symbol.V("F")).ShouldBeFalse();
        }

        [Fact]
        public void FirstEdgeCases()
        {
            Grammar.First(Symbol.Epsilon).ShouldBeEmpty();
            Grammar.First(Symbol.Eof<Sym>()).ShouldSetEqual(Symbol.Eof<Sym>());
        }

        [Fact]
        public void First()
        {
            Grammar.First(Symbol.V("S")).ShouldSetEqual(Symbol.Ts(Sym.LPARAN, Sym.MINUS, Sym.ID));
            Grammar.First(Symbol.V("E")).ShouldSetEqual(Symbol.T(Sym.LPARAN), Symbol.T(Sym.MINUS), Symbol.T(Sym.ID));
            Grammar.First(Symbol.V("T")).ShouldSetEqual(Symbol.T(Sym.LPARAN), Symbol.T(Sym.MINUS), Symbol.T(Sym.ID));
            Grammar.First(Symbol.V("F")).ShouldSetEqual(Symbol.T(Sym.LPARAN), Symbol.T(Sym.MINUS), Symbol.T(Sym.ID));

            Grammar.First(0).ShouldSetEqual(Symbol.Ts(Sym.LPARAN, Sym.MINUS, Sym.ID));
            Grammar.First(1).ShouldSetEqual(Symbol.Ts(Sym.LPARAN, Sym.MINUS, Sym.ID));
            Grammar.First(2).ShouldSetEqual(Symbol.Ts(Sym.LPARAN, Sym.MINUS, Sym.ID));
            Grammar.First(3).ShouldSetEqual(Symbol.Ts(Sym.LPARAN, Sym.MINUS, Sym.ID));
            Grammar.First(4).ShouldSetEqual(Symbol.Ts(Sym.LPARAN, Sym.MINUS, Sym.ID));
            Grammar.First(5).ShouldSetEqual(Symbol.T(Sym.LPARAN));
            Grammar.First(6).ShouldSetEqual(Symbol.T(Sym.MINUS));
            Grammar.First(7).ShouldSetEqual(Symbol.T(Sym.ID));
        }

        [Fact]
        public void Follow()
        {
            Grammar.Follow(Symbol.V("S")).ShouldBeEmpty();
            Grammar.Follow(Symbol.V("E")).ShouldSetEqual(Symbol.Ts(Sym.PLUS, Sym.RPARAN, Sym.EOF));
            Grammar.Follow(Symbol.V("T")).ShouldSetEqual(Symbol.Ts(Sym.PLUS, Sym.ASTERISK, Sym.RPARAN, Sym.EOF));
            Grammar.Follow(Symbol.V("F")).ShouldSetEqual(Symbol.Ts(Sym.PLUS, Sym.ASTERISK, Sym.RPARAN, Sym.EOF));
        }
    }
}
