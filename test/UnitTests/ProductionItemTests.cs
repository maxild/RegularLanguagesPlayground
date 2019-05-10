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

            // LR(0) items
            new ProductionItem<Nonterminal, Terminal>(production, 0, 0).ToString().ShouldBe("E → •E+E");
            new ProductionItem<Nonterminal, Terminal>(production, 0, 1).ToString().ShouldBe("E → E•+E");
            new ProductionItem<Nonterminal, Terminal>(production, 0, 2).ToString().ShouldBe("E → E+•E");
            new ProductionItem<Nonterminal, Terminal>(production, 0, 3).ToString().ShouldBe("E → E+E•");

            // LR(1) items
            new ProductionItem<Nonterminal, Terminal>(production, 0, 0, Symbol.EofMarker).ToString().ShouldBe("[E → •E+E, $]");
            new ProductionItem<Nonterminal, Terminal>(production, 0, 1, Symbol.T('a'), Symbol.EofMarker).ToString().ShouldBe("[E → E•+E, a/$]");

            // Exceptions
            Assert.Throws<ArgumentException>(() => new ProductionItem<Nonterminal, Terminal>(production, 0, 4));
        }
    }
}
