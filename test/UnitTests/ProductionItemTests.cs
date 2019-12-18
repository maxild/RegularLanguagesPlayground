using System;
using System.Diagnostics.CodeAnalysis;
using AutomataLib;
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
            EOF,
            PLUS,       // +
            ID
        }

        enum Var
        {
            E
        }

        public ProductionItemTests()
        {
            T = EnumUtils.MapToSymbolCache<Sym, Terminal<Sym>>((name, index, kind) =>
                new Terminal<Sym>(name, index, kind));

            V = EnumUtils.MapToSymbolCache<Var, Nonterminal>((name, index, _) =>
                new Nonterminal(name, index, typeof(Var)));
        }

        private SymbolCache<Sym, Terminal<Sym>> T { get; }

        private SymbolCache<Var, Nonterminal> V { get; }

        [Fact]
        public void StringifyItems()
        {
            Production production = V[Var.E].Derives(V[Var.E], T[Sym.PLUS], V[Var.E]);

            // LR(0) items
            new ProductionItem<Sym>(production, 0, 0).ToString().ShouldBe("E → •E PLUS E"); // •E+E
            new ProductionItem<Sym>(production, 0, 1).ToString().ShouldBe("E → E•PLUS E");  // E•+E
            new ProductionItem<Sym>(production, 0, 2).ToString().ShouldBe("E → E PLUS•E");  // E+•E
            new ProductionItem<Sym>(production, 0, 3).ToString().ShouldBe("E → E PLUS E•"); // E+E•

            // LR(1) items
            new ProductionItem<Sym>(production, 0, 0, T[Sym.EOF]).ToString().ShouldBe("[E → •E PLUS E, EOF]");
            new ProductionItem<Sym>(production, 0, 1, T[Sym.ID], T[Sym.EOF]).ToString().ShouldBe("[E → E•PLUS E, ID/EOF]");

            // Exceptions
            Assert.Throws<ArgumentException>(() => new ProductionItem<Sym>(production, 0, 4));
        }

        [Fact]
        public void EpsilonProductionGeneratesSingleItem()
        {
            Production epsilonProduction = V[Var.E].Derives(Symbol.Epsilon);
            new ProductionItem<Sym>(epsilonProduction, 0, 0).ToString().ShouldBe("E → •");
            Assert.Throws<ArgumentException>(() =>
                new ProductionItem<Sym>(epsilonProduction, 0, 1).ToString().ShouldBe("E → •"));
        }
    }
}
