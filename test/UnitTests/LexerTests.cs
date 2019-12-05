using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Shouldly;
using UnitTests.Lexers;
using Xunit;
using CsLexSym = UnitTests.Lexers.Sym;

namespace UnitTests
{
    public class LexerTests
    {
        [Fact]
        public void EmptyStringHashCodeIsNotZero()
        {
            string.Empty.GetHashCode().ShouldNotBe(0);
        }

        /// <summary>
        /// Symbol enum for handcoded fake lexer.
        /// </summary>
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        enum Sym
        {
            EOF = 0,
            LPARAN,
            RPARAN,
            ID,
            NUM,
            PLUS,
            MINUS
        }

        [Fact]
        public void FakeLexer()
        {
            // '(a + b) - 100' using fake lexer
            var sut = new FakeLexer<Sym>((Sym.LPARAN, ""), (Sym.ID, "a"), (Sym.PLUS, ""), (Sym.ID, "b"),
                (Sym.RPARAN, ""), (Sym.MINUS, ""), (Sym.NUM, "100"));

            sut.GetNextToken().ShouldBe(new Token<Sym>(Sym.LPARAN));
            sut.GetNextToken().ShouldBe(new Token<Sym>(Sym.ID, "a"));
            sut.GetNextToken().ShouldBe(new Token<Sym>(Sym.PLUS));
            sut.GetNextToken().ShouldBe(new Token<Sym>(Sym.ID, "b"));
            sut.GetNextToken().ShouldBe(new Token<Sym>(Sym.RPARAN));
            sut.GetNextToken().ShouldBe(new Token<Sym>(Sym.MINUS));
            sut.GetNextToken().ShouldBe(new Token<Sym>(Sym.NUM, "100"));
            sut.GetNextToken().ShouldBe(Token<Sym>.EOF);
        }

        [Fact]
        public void CsLexGeneratedLexer()
        {
            // '(a + b) - 100' using DFA-lexer
            var sut = new CalcLexer(new StringReader("(a + b) - 100"));

            sut.GetNextToken().ShouldBe(new Token<CsLexSym>(CsLexSym.LPARAN));
            sut.GetNextToken().ShouldBe(new Token<CsLexSym>(CsLexSym.ID, "a"));
            sut.GetNextToken().ShouldBe(new Token<CsLexSym>(CsLexSym.PLUS));
            sut.GetNextToken().ShouldBe(new Token<CsLexSym>(CsLexSym.ID, "b"));
            sut.GetNextToken().ShouldBe(new Token<CsLexSym>(CsLexSym.RPARAN));
            sut.GetNextToken().ShouldBe(new Token<CsLexSym>(CsLexSym.MINUS));
            sut.GetNextToken().ShouldBe(new Token<CsLexSym>(CsLexSym.NUM, "100"));
            sut.GetNextToken().ShouldBe(Token<CsLexSym>.EOF);
        }
    }

    public interface ILexer<TEnum>
        where TEnum : Enum
    {
        Token<TEnum> GetNextToken();
    }

    public class FakeLexer<TEnum> : ILexer<TEnum> where TEnum : Enum
    {
        private readonly Token<TEnum>[] _tokens;
        private int _index;

        public FakeLexer(IEnumerable<(TEnum, string)> tokenStream)
        {
            _tokens = (tokenStream ?? Enumerable.Empty<(TEnum, string)>())
                .Select(pair => new Token<TEnum>(pair.Item1, pair.Item2)).ToArray();
        }

        public FakeLexer(params (TEnum, string)[] tokenStream)
        {
            _tokens = (tokenStream ?? Enumerable.Empty<(TEnum, string)>())
                .Select(pair => new Token<TEnum>(pair.Item1, pair.Item2)).ToArray();
        }

        public Token<TEnum> GetNextToken()
        {
            return _index < _tokens.Length
                ? _tokens[_index++]
                : Token<TEnum>.EOF;
        }
    }

    public enum TokenComparison
    {
        KindAndLexemeValue,
        KindOnly
    }

    /// <summary>
    /// Test Specification: Always use enum for tokens, Equals/GetHashCode compares every field, Op==/Op!= only compares Kind/TEnum field.
    ///                     EOF kind is assumed to be zero (convention)
    ///                     No location span information.
    /// </summary>
    /// <typeparam name="TEnum"></typeparam>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public struct Token<TEnum> : IEquatable<Token<TEnum>>
        where TEnum : Enum
    {
        // TODO: Can we do better here
        public static readonly Token<TEnum> EOF = new Token<TEnum>((TEnum)Enum.Parse(typeof(TEnum), "EOF"));
        public static readonly Token<TEnum> EPS = new Token<TEnum>(default);

        /// <summary>
        /// Token identifier.
        /// </summary>
        public readonly TEnum Kind; // TODO: Maybe rename to Symbol

        /// <summary>
        /// Lexeme value.
        /// </summary>
        public readonly string Text; // TODO: Maybe rename to LexemeValue

        /// <summary>
        /// Token without a lexeme value.
        /// </summary>
        public Token(TEnum kind)
        {
            Kind = kind;
            Text = string.Empty;
        }

        /// <summary>
        /// Token with a lexeme value.
        /// </summary>
        public Token(TEnum kind, string lexemeValue)
        {
            Kind = kind;
            Text = lexemeValue;
        }

        public override string ToString()
        {
            return $"({Kind}, \"{Text}\")";
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Token<TEnum>)) return false;
            var other = (Token<TEnum>) obj;
            return Equals(other);
        }

        public bool Equals(Token<TEnum> other)
        {
            return Equals(other, TokenComparison.KindAndLexemeValue);
        }

        public bool Equals(Token<TEnum> other, TokenComparison comparison)
        {
            switch (comparison)
            {
                case TokenComparison.KindOnly:
                    return EqualityComparer<TEnum>.Default.Equals(Kind, other.Kind);
                case TokenComparison.KindAndLexemeValue:
                default:
                    return EqualityComparer<TEnum>.Default.Equals(Kind, other.Kind) &&
                           Text.Equals(other.Text, StringComparison.Ordinal);
            }
        }

        public static bool operator ==(Token<TEnum> a, Token<TEnum> b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Token<TEnum> a, Token<TEnum> b)
        {
            return !a.Equals(b);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Kind.GetHashCode();
                if (!string.IsNullOrEmpty(Text))
                    hash = hash * 29 + Text.GetHashCode();
                return hash;
            }
        }
    }
}
