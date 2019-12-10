using System.IO;
using AutomataLib;
using ContextFreeGrammar.Lexers;
using Shouldly;
using UnitTests.Lexers;
using Xunit;
using Sym = UnitTests.Lexers.Sym;

namespace UnitTests
{
    public class LexerTests
    {
        [Fact]
        public void EmptyStringHashCodeIsNotZero()
        {
            string.Empty.GetHashCode().ShouldNotBe(0);
        }

        // This type will probably be generated from a YACC-like tool (and therefore imported into the LEX-like tool generated lexer)
        //enum Sym2
        //{
        //    // NOTE: Values can overlap!!!!
        //    EOF = 0,
        //    EPS = 0,
        //    // EOF = 1 // but names are unique (checked by the C# compiler)
        //}

        [Fact]
        public void FakeLexer()
        {
            // '(a + b) - 100' using fake lexer
            var sut = new FakeLexer<Sym>((Sym.LPARAN, "("), (Sym.ID, "a"), (Sym.PLUS, "+"), (Sym.ID, "b"),
                (Sym.RPARAN, ")"), (Sym.MINUS, "-"), (Sym.NUM, "100"));

            sut.GetNextToken().ShouldBe(new Token<Sym>(Sym.LPARAN, "("));
            sut.GetNextToken().ShouldBe(new Token<Sym>(Sym.ID, "a"));
            sut.GetNextToken().ShouldBe(new Token<Sym>(Sym.PLUS, "+"));
            sut.GetNextToken().ShouldBe(new Token<Sym>(Sym.ID, "b"));
            sut.GetNextToken().ShouldBe(new Token<Sym>(Sym.RPARAN, ")"));
            sut.GetNextToken().ShouldBe(new Token<Sym>(Sym.MINUS, "-"));
            sut.GetNextToken().ShouldBe(new Token<Sym>(Sym.NUM, "100"));
            sut.GetNextToken().ShouldBe(Token<Sym>.EOF);
        }

        [Fact]
        public void CsLexGeneratedLexer()
        {
            // '(a + b) - 100' using DFA-lexer
            var sut = new CalcLexer(new StringReader("(a + b) - 100"));

            sut.GetNextToken().ShouldBe(new Token<Sym>(Sym.LPARAN, "("));
            sut.GetNextToken().ShouldBe(new Token<Sym>(Sym.ID, "a"));
            sut.GetNextToken().ShouldBe(new Token<Sym>(Sym.PLUS, "+"));
            sut.GetNextToken().ShouldBe(new Token<Sym>(Sym.ID, "b"));
            sut.GetNextToken().ShouldBe(new Token<Sym>(Sym.RPARAN, ")"));
            sut.GetNextToken().ShouldBe(new Token<Sym>(Sym.MINUS, "-"));
            sut.GetNextToken().ShouldBe(new Token<Sym>(Sym.NUM, "100"));
            sut.GetNextToken().ShouldBe(Token<Sym>.EOF);
        }
    }
}
