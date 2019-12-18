using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using AutomataLib;

namespace ContextFreeGrammar
{
    /// <summary>
    /// Represents a grammar symbol (terminal, nonterminal) or one of the
    /// two reserved symbols EOF marker or Epsilon (the empty string).
    /// </summary>
    /// <remarks>
    /// The two reserved symbols are not grammar symbols. They are reserved keywords in the grammar of grammars
    /// that we for practical reasons describe as symbols, because it makes sense a lot of places in the code.
    /// </remarks>
    public abstract class Symbol : IEquatable<Symbol>
    {
        // NOTE: Because Nonterminal does not contain TEnum nonterminal kind parameter we have
        // to use the Type of the Kind to implement Equals correctly.
        private readonly Type _kindType;

        protected Symbol(string name, int index, Type kindType)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(name);
            }
            Name = name;
            Index = index;
            _kindType = kindType ?? throw new ArgumentNullException(nameof(kindType));
        }

        /// <summary>
        /// The name of the (grammar) variable (nonterminal) in the BNF, or the name of the terminal symbol (i.e. the name of some abstract
        /// input symbol, aka token kind, lexical unit, identified by the lexer). Both interpretations of name (for variable or terminal)
        /// are what the parser processes during derivations/reductions of the grammar.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Get the symbols unique identifier that also happen to be an index in the range 0,1,2,...,N.
        /// </summary>
        /// <remarks>
        /// The <see cref="Epsilon"/> symbol will get an index equal to -1. This is because the empty string is not a
        /// grammar symbol that will be used in any parsing table.
        /// </remarks>
        public int Index { get; }

        /// <summary>
        /// Is the symbol a terminal symbol that is either part of the alphabet of the language or
        /// is it the reserved EOF marker symbol.
        /// In other words, is the symbol part of the input language of the parser (or equivalently the
        /// output language of the lexer).
        /// </summary>
        /// <remarks>
        /// In textbooks the extended terminal symbols is defined by the set T' = T ∪ {$}, that contain
        /// all terminal symbols and the reserved eof marker symbol.
        /// If <see cref="IsExtendedTerminal"/> is equal to <c>true</c>, then we can safely downcast the symbol to
        /// the <see cref="Terminal{TTokenKind}"/> subclass. That is all <see cref="Terminal{TTokenKind}"/> derived symbols have
        /// <see cref="IsExtendedTerminal"/> equal to <c>true</c>.
        /// </remarks>
        public abstract bool IsExtendedTerminal { get; }

        /// <summary>
        /// Is the symbol a terminal symbol that is part of the language described by the grammar (i.e. the alphabet of the language).
        /// </summary>
        /// <remarks>
        /// All <see cref="Terminal{TTokenKind}"/> symbols have <see cref="IsExtendedTerminal"/> equal to <c>true</c>.
        /// All <see cref="Terminal{TTokenKind}"/> symbols except one value have <see cref="IsTerminal"/> equal to <c>true</c>.
        /// The only <see cref="Terminal{TTokenKind}"/> value that does not have <see cref="IsTerminal"/> equal to <c>true</c> is the
        /// reserved EOF marker symbol"/>.
        /// </remarks>
        public abstract bool IsTerminal { get; }

        /// <summary>
        /// Is the symbol a variable used in the language specification (BNF, EBNF or similar) to describe
        /// one or more rewrite rules. A nonterminal also describes an internal node in the derivation (parse) tree
        /// produced by the parser. Therefore is plays a role in when transforming the parse tree into an abstract
        /// syntax tree, because it governs semantic actions.
        /// </summary>
        public abstract bool IsNonterminal { get; }

        /// <summary>
        /// Is the symbol the empty symbol.
        /// </summary>
        /// <remarks>
        /// This is not exactly the same as the empty string. We often use this special value
        /// when a function cannot return a symbol, because the symbol does not exist. An example
        /// is returning the dot symbol of a final (reduce) item.
        /// </remarks>
        public abstract bool IsEpsilon { get; }

        /// <summary>
        /// Is the symbol the end of input.
        /// </summary>
        /// <remarks>
        /// The lexer will return this symbol to indicate end of input to the parser.
        /// </remarks>
        public abstract bool IsEof { get; }

        /// <summary>
        /// Reserved symbol 'ε' representing the empty string, that is only used as a reserved symbol in the grammar of
        /// grammars (just like the '→' symbol).
        /// </summary>
        /// <remarks>
        /// A production is a relation V → (V∪T)∗. The Kleene star covers all productions on the form A → ε.
        /// Therefore the empty string is not a terminal symbol. In fact is not a symbol at all, but it represents
        /// missing symbol. It is sort of like the empty list, or the Nothing of the Maybe in Haskell. In C# we
        /// represent the empty string with a so-called null object.
        /// </remarks>
        public static readonly Symbol Epsilon = new Eps();

        /// <summary>
        /// The empty string is just a reserved symbol in the (meta-)language for grammars. It is used to make it explicit
        /// that a production has an empty RHS. Such a production (A → ε) is known as an ε-production.
        /// The reserved symbol 'ε' is just like the reserved symbol '→' a special keyword in the grammar for a grammar
        /// (our so-called metalanguage). We represent it in the library as a (reserved) <see cref="Symbol"/> derived singleton
        /// It is not part of any grammar, and therefore it is not a terminal symbol, and it is not a nonterminal symbol either.
        /// </summary>
        class Eps : Symbol
        {
            public Eps() : base("ε", index: -1, typeof(Eps))
            {
            }

            public override bool IsExtendedTerminal => false;

            public override bool IsTerminal => false;

            public override bool IsNonterminal => false;

            public override bool IsEpsilon => true;

            public override bool IsEof => false;
        }

        public override string ToString()
        {
            return Name;
        }

        public bool Equals(Symbol other)
        {
            // Same named constant (i.e. index) of same underlying enumeration (i.e. kind)
            return other != null && Index == other.Index && _kindType == other._kindType;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Symbol))
                return false;
            return Equals((Symbol) obj);
        }

        public override int GetHashCode()
        {
            return Index;
        }
    }

    /// <summary>
    /// A nonterminal (aka grammar variable) in V.
    /// </summary>
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
    public class Nonterminal : Symbol, IEquatable<Nonterminal>, ISymbolIndex
    {
        private string DebuggerDisplay => Name;

        public Nonterminal(string name, int index, Type kindType)
            : base(name, index, kindType)
        {
        }

        public override bool IsExtendedTerminal => false;

        public override bool IsTerminal => false;

        public override bool IsNonterminal => true;

        public override bool IsEpsilon => false;

        public override bool IsEof => false;

        public bool Equals(Nonterminal other)
        {
            return base.Equals(other);
        }

        public Production Derives(params Symbol[] tail)
        {
            return new Production(this, tail);
        }

        public Production DerivesEpsilon()
        {
            return new Production(this, Enumerable.Empty<Symbol>());
        }
    }

    /// <summary>
    /// A terminal in T defined by some token kind enumeration (enum from corefx).
    /// </summary>
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class Terminal<TTokenKind> : Symbol, IEquatable<Terminal<TTokenKind>>, ISymbolIndex
        where TTokenKind : struct, Enum
    {
        private string DebuggerDisplay => Name;

        public Terminal(string name, int index, TTokenKind kind)
            : base(name, index, typeof(TTokenKind))
        {
            Kind = kind;
        }

        public TTokenKind Kind { get; }

        public override bool IsExtendedTerminal => true;

        public override bool IsTerminal => !IsEof; // By convention EOF is not a terminal

        public override bool IsNonterminal => false;

        public override bool IsEpsilon => false;

        // TODO: At index zero, because of enforced convention
        public override bool IsEof => Name.Equals(TokenKinds.Eof, StringComparison.Ordinal);

        public bool Equals(Terminal<TTokenKind> other)
        {
            return base.Equals(other);
        }
    }
}
