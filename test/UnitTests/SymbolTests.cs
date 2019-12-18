using System.Diagnostics.CodeAnalysis;
using AutomataLib;
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

        enum Var
        {
            S
        }

        public SymbolTests()
        {
            T = EnumUtils.MapToSymbolCache<Sym, Terminal<Sym>>((name, index, kind) =>
                new Terminal<Sym>(name, index, kind));

            V = EnumUtils.MapToSymbolCache<Var, Nonterminal>((name, index, _) =>
                new Nonterminal(name, index, typeof(Var)));
        }

        private SymbolCache<Sym, Terminal<Sym>> T { get; }

        private SymbolCache<Var, Nonterminal> V { get; }

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
            T[Sym.EOF].IsExtendedTerminal.ShouldBeTrue();
            T[Sym.EOF].IsTerminal.ShouldBeFalse();
            T[Sym.EOF].IsNonterminal.ShouldBeFalse();
            T[Sym.EOF].IsEpsilon.ShouldBeFalse();
        }

        [Fact]
        public void Terminal()
        {
            T[Sym.ID].IsExtendedTerminal.ShouldBeTrue();
            T[Sym.ID].IsTerminal.ShouldBeTrue();
            T[Sym.ID].IsNonterminal.ShouldBeFalse();
            T[Sym.ID].IsEpsilon.ShouldBeFalse();

            T[Sym.PLUS].IsExtendedTerminal.ShouldBeTrue();
            T[Sym.PLUS].IsTerminal.ShouldBeTrue();
            T[Sym.PLUS].IsNonterminal.ShouldBeFalse();
            T[Sym.PLUS].IsEpsilon.ShouldBeFalse();
        }

        [Fact]
        public void Nonterminal()
        {
            V[Var.S].IsExtendedTerminal.ShouldBeFalse();
            V[Var.S].IsTerminal.ShouldBeFalse();
            V[Var.S].IsNonterminal.ShouldBeTrue();
            V[Var.S].IsEpsilon.ShouldBeFalse();
        }
    }
}
