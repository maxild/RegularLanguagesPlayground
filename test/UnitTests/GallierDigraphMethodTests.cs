using System.Linq;
using AutomataLib;
using ContextFreeGrammar;
using ContextFreeGrammar.Analyzers;
using Shouldly;
using Xunit;

namespace UnitTests
{
    public class GallierDigraphMethodTests
    {
        public GallierDigraphMethodTests()
        {
            // Example 1 from the small survey of digraph methods https://www.cis.upenn.edu/~jean/gbooks/graphm.pdf
            // 0: S → E$
            // 1: E → E+T
            // 2: E → T
            // 3: T → T*F
            // 4: T → F
            // 5: F → (E)
            // 6: F → -T
            // 7: F → a
            Grammar = new GrammarBuilder()
                .SetAnalyzer(Analyzers.CreateDigraphAlgorithmAnalyzer)
                .SetNonterminalSymbols(Symbol.Vs("S", "E", "T", "F"))
                .SetTerminalSymbols(Symbol.Ts('a', '+', '-', '*', '(', ')').WithEofMarker())
                .SetStartSymbol(Symbol.V("S"))
                .AndProductions(
                    Symbol.V("S").Derives(Symbol.V("E"), Symbol.EofMarker),
                    Symbol.V("E").Derives(Symbol.V("E"), Symbol.T('+'), Symbol.V("T")),
                    Symbol.V("E").Derives(Symbol.V("T")),
                    Symbol.V("T").Derives(Symbol.V("T"), Symbol.T('*'), Symbol.V("F")),
                    Symbol.V("T").Derives(Symbol.V("F")),
                    Symbol.V("F").Derives(Symbol.T('('), Symbol.V("E"), Symbol.T(')')),
                    Symbol.V("F").Derives(Symbol.T('-'), Symbol.V("T")),
                    Symbol.V("F").Derives(Symbol.T('a'))
                );
        }

        private Grammar<Nonterminal, Terminal> Grammar { get; }

        [Fact]
        public void ErasableEdgeCases()
        {
            Grammar.Erasable(Symbol.Epsilon).ShouldBeTrue();
            Grammar.Erasable(Symbol.EofMarker).ShouldBeTrue();
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
            Grammar.First(Symbol.EofMarker).ShouldSetEqual(new[] { Symbol.EofMarker });
        }

        [Fact]
        public void First()
        {
            Grammar.First(Symbol.V("S")).ShouldSetEqual(Symbol.Ts('(', '-', 'a'));
            Grammar.First(Symbol.V("E")).ShouldSetEqual(Symbol.Ts('(', '-', 'a'));
            Grammar.First(Symbol.V("T")).ShouldSetEqual(Symbol.Ts('(', '-', 'a'));
            Grammar.First(Symbol.V("F")).ShouldSetEqual(Symbol.Ts('(', '-', 'a'));

            Grammar.First(0).ShouldSetEqual(Symbol.Ts('(', '-', 'a'));
            Grammar.First(1).ShouldSetEqual(Symbol.Ts('(', '-', 'a'));
            Grammar.First(2).ShouldSetEqual(Symbol.Ts('(', '-', 'a'));
            Grammar.First(3).ShouldSetEqual(Symbol.Ts('(', '-', 'a'));
            Grammar.First(4).ShouldSetEqual(Symbol.Ts('(', '-', 'a'));
            Grammar.First(5).ShouldSetEqual(Symbol.Ts('('));
            Grammar.First(6).ShouldSetEqual(Symbol.Ts('-'));
            Grammar.First(7).ShouldSetEqual(Symbol.Ts('a'));
        }

        [Fact]
        public void Follow()
        {
            // TODO: INITFIRST(S) is wrong...write tests for init routine
            //Grammar.Follow(Symbol.V("S")).ShouldSetEqual(Symbol.Ts().WithEofMarker());
            Grammar.Follow(Symbol.V("E")).ShouldSetEqual(Symbol.Ts('+', ')').WithEofMarker());
            //Grammar.Follow(Symbol.V("T")).ShouldSetEqual(Symbol.Ts('+', '*', ')').WithEofMarker());
            //Grammar.Follow(Symbol.V("F")).ShouldSetEqual(Symbol.Ts('+', '*', ')').WithEofMarker());
        }
    }
}
