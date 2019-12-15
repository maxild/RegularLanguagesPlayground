using System;
using System.Diagnostics.CodeAnalysis;
using ContextFreeGrammar;
using Shouldly;
using Xunit;

namespace UnitTests
{
    public class ProductionItemTests
    {
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        enum Sym
        {
            PLUS,       // +
            ID,
            EOF
        }

        [Fact]
        public void StringifyItems()
        {
            Production production = Symbol.V("E").Derives(Symbol.V("E"), Symbol.T(Sym.PLUS), Symbol.V("E"));

            // LR(0) items
            new ProductionItem<Sym>(production, 0, 0).ToString().ShouldBe("E → •E PLUS E"); // •E+E
            new ProductionItem<Sym>(production, 0, 1).ToString().ShouldBe("E → E•PLUS E");  // E•+E
            new ProductionItem<Sym>(production, 0, 2).ToString().ShouldBe("E → E PLUS•E");  // E+•E
            new ProductionItem<Sym>(production, 0, 3).ToString().ShouldBe("E → E PLUS E•"); // E+E•

            // LR(1) items
            new ProductionItem<Sym>(production, 0, 0, Symbol.Eof<Sym>()).ToString().ShouldBe("[E → •E PLUS E, EOF]");
            new ProductionItem<Sym>(production, 0, 1, Symbol.T(Sym.ID), Symbol.Eof<Sym>()).ToString().ShouldBe("[E → E•PLUS E, ID/EOF]");

            // Exceptions
            Assert.Throws<ArgumentException>(() => new ProductionItem<Sym>(production, 0, 4));
        }

        [Fact]
        public void EpsilonProductionGeneratesSingleItem()
        {
            Production epsilonProduction = Symbol.V("E").Derives(Symbol.Epsilon);
            new ProductionItem<Sym>(epsilonProduction, 0, 0).ToString().ShouldBe("E → •");
            Assert.Throws<ArgumentException>(() =>
                new ProductionItem<Sym>(epsilonProduction, 0, 1).ToString().ShouldBe("E → •"));
        }
    }
}
