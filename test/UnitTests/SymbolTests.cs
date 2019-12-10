using System.Diagnostics.CodeAnalysis;
using AutomataLib;
using Shouldly;
using Xunit;

namespace UnitTests
{
    public class SymbolTests
    {
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        enum Sym
        {
            EPS = 0,
            PLUS,       // +
            ID,
            EOF
        }

        [Fact]
        public void Epsilon()
        {
            Symbol.Epsilon.IsExtendedTerminal.ShouldBeFalse();
            Symbol.Epsilon.IsTerminal.ShouldBeFalse();
            Symbol.Epsilon.IsNonterminal.ShouldBeFalse();
            Symbol.Epsilon.IsEpsilon.ShouldBeTrue();
        }

        [Fact]
        public void Eof()
        {
            Symbol.Eof<Sym>().IsExtendedTerminal.ShouldBeTrue();
            Symbol.Eof<Sym>().IsTerminal.ShouldBeFalse();
            Symbol.Eof<Sym>().IsNonterminal.ShouldBeFalse();
            Symbol.Eof<Sym>().IsEpsilon.ShouldBeFalse();
        }

        [Fact]
        public void Terminal()
        {
            Symbol.T(Sym.ID).IsExtendedTerminal.ShouldBeTrue();
            Symbol.T(Sym.ID).IsTerminal.ShouldBeTrue();
            Symbol.T(Sym.ID).IsNonterminal.ShouldBeFalse();
            Symbol.T(Sym.ID).IsEpsilon.ShouldBeFalse();

            Symbol.T(Sym.PLUS).IsExtendedTerminal.ShouldBeTrue();
            Symbol.T(Sym.PLUS).IsTerminal.ShouldBeTrue();
            Symbol.T(Sym.PLUS).IsNonterminal.ShouldBeFalse();
            Symbol.T(Sym.PLUS).IsEpsilon.ShouldBeFalse();
        }

        [Fact]
        public void Nonterminal()
        {
            Symbol.V("S").IsExtendedTerminal.ShouldBeFalse();
            Symbol.V("S").IsTerminal.ShouldBeFalse();
            Symbol.V("S").IsNonterminal.ShouldBeTrue();
            Symbol.V("S").IsEpsilon.ShouldBeFalse();
        }
    }
}
