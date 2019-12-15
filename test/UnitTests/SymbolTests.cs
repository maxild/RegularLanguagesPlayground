using System.Diagnostics.CodeAnalysis;
using ContextFreeGrammar;
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
            // Hidden token kinds have negative values
            NIL = -2,   // Lexical error (syntax error)
            EPS = -1,   // Epsilon (empty/hidden) token
            // All non-hidden token kinds are sequentially ordered 0,1,2,...,N-1
            EOF,        // EOF marker
            PLUS,       // +
            ID
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
