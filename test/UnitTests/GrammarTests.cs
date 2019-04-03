using System;
using ContextFreeGrammar;
using Shouldly;
using Xunit;

namespace UnitTests
{
    public class GrammarTests
    {
        [Fact]
        public void Test()
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
            var grammar = new Grammar(Symbol.Vs("S", "E"), Symbol.Ts('a', 'b'), Symbol.V("S"))
            {
                Symbol.V("S").GoesTo(Symbol.V("E")),
                Symbol.V("E").GoesTo(Symbol.T('a'), Symbol.V("E"), Symbol.T('b')),
                Symbol.V("E").GoesTo(Symbol.T('a'), Symbol.T('b'))
            };

            grammar.ToString().ShouldBe(@"0: S → E
1: E → aEb
2: E → ab
");

            // Create LR(0) Automaton

            // Create states
        }
    }

    public class ProductionItemTests
    {
        [Fact]
        public void Test()
        {
            var production = Symbol.V("E").GoesTo(Symbol.V("E"), Symbol.T('+'), Symbol.V("E"));
            new ProductionItem(production, 0, 0).ToString().ShouldBe("E → •E+E");
            new ProductionItem(production, 0, 1).ToString().ShouldBe("E → E•+E");
            new ProductionItem(production, 0, 2).ToString().ShouldBe("E → E+•E");
            new ProductionItem(production, 0, 3).ToString().ShouldBe("E → E+E•");
            Assert.Throws<ArgumentException>(() => new ProductionItem(production, 0, 4));
        }
    }

    public static class GrammarExtensions
    {
        public static Production GoesTo(this NonTerminal head, params Symbol[] tail)
        {
            return new Production(head, tail);
        }
    }
}
