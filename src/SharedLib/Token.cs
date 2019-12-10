using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace AutomataLib
{
    // PROBLEM: where TTerminalSymbol : Symbol, IEquatable<TTerminalSymbol>
    //
    //   where TTerminalSymbol : IGrammarSymbol, IEquatable<TTerminalSymbol>
    //
    // ILexer is parameterized by TEnum not Token<TEnum>, and therefore does TTerminalSymbol not flow....


    // This is the interface with the lexer/tokenizer. Token is a value type (for better performance)

    // Symbol and Terminal are different types, that only share the TTokenKind enum. Terminal describe the LHS of grammar rules
    // and _not_ the interface with the lexer. Also Symbol and Symbol-derived types are reference types.

    /// <summary>
    /// Test Specification: Always use enum for tokens, Equals/GetHashCode compares every field, Op==/Op!= only compares Kind/TEnum field.
    ///                     EOF kind is assumed to be zero (convention)
    ///                     No location span information.
    /// </summary>
    /// <typeparam name="TEnum">The token kind enumeration.</typeparam>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public struct Token<TEnum> : IEquatable<Token<TEnum>>
        where TEnum : Enum
    {
        // TODO: Can we do better here
        public static readonly Token<TEnum> EOF = new Token<TEnum>((TEnum)Enum.Parse(typeof(TEnum), "EOF"), "$");
        public static readonly Token<TEnum> EPS = new Token<TEnum>(default);

        /// <summary>
        /// The name of the lexical unit.
        /// </summary>
        public string Name => Enum.GetName(typeof(TEnum), Kind);

        public int Index => Convert.ToInt32(Kind);

        public bool IsExtendedTerminal => true;

        public bool IsTerminal => !IsEof;

        public bool IsNonterminal => false;

        public bool IsEpsilon => Equals(EPS);

        public bool IsEof => Equals(EOF);

        /// <summary>
        /// The type of the lexical unit.
        /// </summary>
        public readonly TEnum Kind; // TODO: Maybe rename to Symbol or TokenKind

        /// <summary>
        /// Lexeme value.
        /// </summary>
        public readonly string Text; // TODO: Maybe rename to LexemeValue

        // Parsed simple values are a lexer responsibility (integer, bool etc.)
        // TODO: public TValue Value { get; } // Later on we need this prop to ease semantic actions

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
            var other = (Token<TEnum>)obj;
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

    public enum TokenComparison
    {
        KindAndLexemeValue,
        KindOnly
    }
}
