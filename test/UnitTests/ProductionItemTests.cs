using System;
using AutomataLib;
using ContextFreeGrammar;
using Shouldly;
using Xunit;

namespace UnitTests
{
    public class ProductionItemTests
    {
        [Fact]
        public void StringifyItems()
        {
            Production<Nonterminal> production = Symbol.V("E").GoesTo(Symbol.V("E"), Symbol.T('+'), Symbol.V("E"));
            new ProductionItem<Nonterminal>(production, 0, 0).ToString().ShouldBe("E → •E+E");
            new ProductionItem<Nonterminal>(production, 0, 1).ToString().ShouldBe("E → E•+E");
            new ProductionItem<Nonterminal>(production, 0, 2).ToString().ShouldBe("E → E+•E");
            new ProductionItem<Nonterminal>(production, 0, 3).ToString().ShouldBe("E → E+E•");
            Assert.Throws<ArgumentException>(() => new ProductionItem<Nonterminal>(production, 0, 4));
        }
    }
}
