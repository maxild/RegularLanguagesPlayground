using AutomataLib;
using Shouldly;
using Xunit;

namespace UnitTests
{
    public class SymbolTests
    {
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
            Symbol.EofMarker.IsExtendedTerminal.ShouldBeTrue();
            Symbol.EofMarker.IsTerminal.ShouldBeFalse();
            Symbol.EofMarker.IsNonterminal.ShouldBeFalse();
            Symbol.EofMarker.IsEpsilon.ShouldBeFalse();
        }

        [Fact]
        public void Terminal()
        {
            Symbol.T('a').IsExtendedTerminal.ShouldBeTrue();
            Symbol.T('a').IsTerminal.ShouldBeTrue();
            Symbol.T('a').IsNonterminal.ShouldBeFalse();
            Symbol.T('a').IsEpsilon.ShouldBeFalse();
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
