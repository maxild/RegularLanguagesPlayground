using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace AutomataLib
{
    // Token and Terminal are different types, that only share the TTokenKind enum. Terminal describe
    // the LHS of grammar rules and are _not_ the interface with the lexer. Token describe the interface
    // with the lexer and is very much concerned with the user input to the compiler pipeline (kind, span etc)

    // Terminal<TTokenKind>
    //    * It is a grammar symbol, and therefore can be the spelling property of a state (LR(k) item set)
    //    * The is a terminal symbol for each token kind (where epsilon/empty token kind is the exception)
    //    * Each token can be mapped to a terminal symbol (via table/array indexing)
    //    * A terminal symbol has an index property.
    //
    // Token<TTokenKind>
    //    * The token is the input into the parser (and the interface with the lexer)
    //    * There are many more tokens than there are token kinds (terminal symbols)
    //    * Each token represent a 'span' of the input, has a lexeme value, and also belong
    //      to a token kind, that represents a lexical class/category of input defined by a RE pattern.
    //    * The token is put on the value stack (not the same as the parser stack) because the semantic
    //      actions of a (reduced) rule need to receive the token lexeme value as input for every terminal
    //      symbol on the RHS of the rule.

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public struct Token<TTokenKind> : IEquatable<Token<TTokenKind>>
        where TTokenKind : Enum
    {
        /// <summary>
        /// Reserved token that represents the end-of-input.
        /// </summary>
        public static readonly Token<TTokenKind> EOF = new Token<TTokenKind>((TTokenKind)Enum.Parse(typeof(TTokenKind), "EOF"), "$");

        /// <summary>
        /// Reserved token that represents the empty token (epsilon, nil, null, empty, or whatnot).
        /// </summary>
        /// <remarks>
        /// This value should only be used in the lexer to signal 'hidden tokens' (aka trivia),
        /// and should never be send to the parser. We define here for this reason, such that lexer actions
        /// can return default/null values to the lexer when ignoring whitespace, and other hidden tokens.
        /// </remarks>
        public static readonly Token<TTokenKind> EPS = new Token<TTokenKind>(default);

        /// <summary>
        /// The name of the lexical unit.
        /// </summary>
        public string Name => Enum.GetName(typeof(TTokenKind), Kind);

        public bool IsEof => Equals(EOF);

        /// <summary>
        /// The type of the lexical unit.
        /// </summary>
        public readonly TTokenKind Kind;

        /// <summary>
        /// Lexeme value.
        /// </summary>
        public readonly string Text;

        // Parsed simple values are a lexer responsibility (integer, bool etc.)
        // TODO: public TValue Value { get; } // Later on we need this prop to ease semantic actions

        /// <summary>
        /// Token without a lexeme value.
        /// </summary>
        public Token(TTokenKind kind)
        {
            Kind = kind;
            Text = string.Empty;
        }

        /// <summary>
        /// Token with a lexeme value.
        /// </summary>
        public Token(TTokenKind kind, string lexemeValue)
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
            if (!(obj is Token<TTokenKind>)) return false;
            var other = (Token<TTokenKind>)obj;
            return Equals(other);
        }

        public bool Equals(Token<TTokenKind> other)
        {
            return Equals(other, TokenComparison.KindAndLexemeValue);
        }

        public bool Equals(Token<TTokenKind> other, TokenComparison comparison)
        {
            switch (comparison)
            {
                case TokenComparison.KindOnly:
                    return EqualityComparer<TTokenKind>.Default.Equals(Kind, other.Kind);
                case TokenComparison.KindAndLexemeValue:
                default:
                    return EqualityComparer<TTokenKind>.Default.Equals(Kind, other.Kind) &&
                           Text.Equals(other.Text, StringComparison.Ordinal);
            }
        }

        public static bool operator ==(Token<TTokenKind> a, Token<TTokenKind> b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Token<TTokenKind> a, Token<TTokenKind> b)
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
