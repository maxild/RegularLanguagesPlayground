using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using AutomataLib;

namespace ContextFreeGrammar
{
    // Conventions (to be validated by grammar type/class):
    //   tokens have to represented by enumeration indexed 0,1,2...,N (table indexes)
    //   terminals are uppercase words: NUM, ID etc...
    //   variables have to be represented by enumeration 0,1,2,...N (table indexes)
    //   nonterminals are lowercase word: expression, term, factor, declaration
    //   terminals are uniquely indexed by enum (maybe generated from grammar spec). EOF and EPS are both
    //   tokens (but only EOF carry over to Symbols).
    //   nonterminals are insertion ordered by their definition in grammar.
    //
    //                       IsExtendedTerminal    IsTerminal    IsNonterminal    IsEpsilon      IsEof
    //       EOF                    true              false          false           false       true
    //       EPS                    true              true           false           true        false
    //       ID/NUM                 true              true           false           false       false
    //       expr/factor            false             false          true            false       false

    // NOTE: Type (int, Node, string) etc is not part yet of grammar (no semantic actions yet)

    // TODO: Create grammar symbol namespace (part of grammar)
    //    terminals defined by TTokenKind enum instance
    //    nonterminals defined by rules (we need | operator)
    // TODO: Maybe better to move all other stuff out of grammar, because grammar (CFG) should only contain
    //      the basic rewriting system (phrase structure ????, generative grammar).

    // TODO: Create TokenKindCache for enum metadata (name, value, index) and use it for both Token and Terminal

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
        protected Symbol(string name, int index)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(name);
            }
            Name = name;
            Index = index;
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
        /// is it the reserved eof marker symbol instantiated by <see cref="Symbol.Eof{TTokenKind}()"/>.
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
        /// All <see cref="Terminal{TTokenKind}"/> derived symbols have <see cref="IsTerminal"/> equal to <c>true</c>. The only
        /// <see cref="Terminal{TTokenKind}"/> value that does not have <see cref="IsTerminal"/> equal to <c>true</c> is the
        /// reserved eof marker symbol instantiated by <see cref="Symbol.Eof{TTokenKind}()"/>.
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
        /// Reserved (extended) terminal symbol for end of input ('$' in dragon book).
        /// </summary>
        /// <remarks>
        /// Many texts on parsing and compiler theory will not agree that the eof marker ($) is a terminal symbol.
        /// In a way this is correct, because the language (per se) cannot contain this token. But in a way 'end
        /// of input' must be communicated from the lexer to the parser some way, and the most elegant (pure)
        /// way, is to extend the input alphabet T with this reserved token: T' = T ∪ {$}.
        /// Any valid grammar will only contain a single production containing the eof marker. This special
        /// production rule is by convention the first production of the grammar. This production
        /// S' → S$ give rise to two kernel items [S' → •S$], the initial item (state 1 in our implementation), and [S' → S•$], the final
        /// accepting item (state 2 in our implementation). This way the special S' → S$ rule is added to the grammar to allow
        /// the parser to accept the input in a deterministic way. That is a bottom-up (left) parser will only accept the input
        /// if the next input token is eof ($) after reducing by the final accept item [S' → S•$].
        /// </remarks>
        public static Terminal<TTokenKind> Eof<TTokenKind>() where TTokenKind : struct, Enum => Terminal<TTokenKind>.EOF;

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
            public Eps() : base("ε", index: -1)
            {
            }

            public override bool IsExtendedTerminal => false;

            public override bool IsTerminal => false;

            public override bool IsNonterminal => false;

            public override bool IsEpsilon => true;

            public override bool IsEof => false;
        }

        // TODO: T, Ts, V and Vs should be moved else where (because we need central repository/cache for singleton values) scoped to a grammar

        // Registry<TKey, TSymbol> == IReadOnlyList<TSymbol> + IndexOf(TKey key)

        // Registry<TTokenKind, Terminal<TTokenKind>>
        //      Enum --> Terminal --> Index         (Enum is the index)
        //      Index --> Terminal --> Enum

        // Registry<string, Nonterminal>
        //      Name --> Nonterminal --> Index      (Here we need insertion ordered set...but we only need IndexOf)
        //      Index --> Nonterminal --> Name

        public static Nonterminal V(string name)
        {
            return new Nonterminal(name);
        }

        public static IEnumerable<Nonterminal> Vs(params string[] names)
        {
            return names.Select(name => new Nonterminal(name));
        }

        public static Terminal<TTokenKind> T<TTokenKind>(TTokenKind kind)
            where TTokenKind : struct, Enum
        {
            return Terminal<TTokenKind>.T(kind);
        }

        public static IEnumerable<Terminal<TTokenKind>> Ts<TTokenKind>(params TTokenKind[] kinds)
            where TTokenKind : struct, Enum
        {
            return kinds.Select(Terminal<TTokenKind>.T);
        }

        public override string ToString()
        {
            return Name;
        }

        public bool Equals(Symbol other)
        {
            if (other == null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            if (GetType() != other.GetType())
                return false;

            // TODO: Uncomment when index is created in Nonterminal
            //return Index == other.Index;
            return Name.Equals(other.Name, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Symbol))
            {
                return false;
            }

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
    public class Nonterminal : Symbol, IEquatable<Nonterminal>
    {
        private string DebuggerDisplay => Name;

        internal Nonterminal(string name)
            : base(name, 0) // TODO: Create index
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
    }

    /// <summary>
    /// A terminal in T defined by some token kind enumeration (enum from corefx).
    /// </summary>
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class Terminal<TTokenKind> : Symbol, IEquatable<Terminal<TTokenKind>>
        where TTokenKind : struct, Enum
    {
        /// <summary>
        /// Reserved terminal grammar symbol for EOF marker.
        /// </summary>
        internal static Terminal<TTokenKind> EOF => All[s_eofIndex];

        // NOTE: The static fields will not be shared between different closed Terminal<TTokenKind> classes. That
        // Terminal<Sym1> and Terminal<Sym2> will have different independent static fields. This is correct for our cache.
        // ReSharper disable once StaticMemberInGenericType
        private static readonly int s_eofIndex;

        // NOTE: the static constructor will be called once for each closed class type (i.e. once for each token kind)
        static Terminal()
        {
            (All, s_eofIndex) = GetSeqOrdTerminals();
        }

        internal static IReadOnlyList<Terminal<TTokenKind>> All { get; }

        internal static Terminal<TTokenKind> T(TTokenKind kind)
        {
            return All[Convert.ToInt32(kind)];
        }

        // TODO: All the validation exceptions are hard to catch when done in static initializers...move vakidations to GrammarBuilder and use intermediate sequence of tuples (kind, name, index)

        /// <summary>
        /// Get sequentially ordered list of token kinds from minimal bound zero to maximal bound N-1.
        /// </summary>
        public static (IReadOnlyList<Terminal<TTokenKind>>, int) GetSeqOrdTerminals()
        {
            Type t = typeof(TTokenKind);

            if (Attribute.IsDefined(t, typeof(FlagsAttribute)))
                throw new InvalidOperationException("The TTokenKind enum type cannot have [Flags] attribute.");
            if (Enum.GetUnderlyingType(t) != typeof(int))
                throw new InvalidOperationException("The TTokenKind enum type must have underlying type equal to System.Int32.");

            int eofIndex = -1;
            List<Terminal<TTokenKind>> terminals = new List<Terminal<TTokenKind>>();

            int index = 0;
            foreach (var tokenKind in ((TTokenKind[])Enum.GetValues(t)).OrderBy(kind => kind))
            {
                int rawValue = Convert.ToInt32(tokenKind);

                // Hidden tokens (epsilon, trivia) have negative value
                if (rawValue < 0)
                    continue;

                if (rawValue != index)
                    throw new InvalidOperationException($"The values of {t.FullName} are not sequentially ordered 0,1,...,N-1.");

                string name = tokenKind.ToString();

                //if (name.Any(c => false == char.IsUpper(c)))
                //    throw new InvalidOperationException(
                //        $"Terminal symbols defined by the names of {t.FullName} must all be uppercase. The name {name} is not.");

                if (TokenKinds.Eof.Equals(name, StringComparison.Ordinal))
                    eofIndex = index;

                terminals.Add(new Terminal<TTokenKind>(tokenKind, name, index));

                index += 1;
            }

            if (eofIndex < 0)
                throw new InvalidOperationException($"The reserved name EOF was not found among the names of {t.FullName}.");

            return (terminals, eofIndex);
        }

        private string DebuggerDisplay => Name;

        private Terminal(TTokenKind kind, string name, int index)
            : base(name, index)
        {
            Kind = kind;
        }

        public TTokenKind Kind { get; }

        public override bool IsExtendedTerminal => true;

        public override bool IsTerminal => !IsEof; // By convention EOF is not a terminal

        public override bool IsNonterminal => false;

        public override bool IsEpsilon => false;

        public override bool IsEof => Equals(EOF);

        public bool Equals(Terminal<TTokenKind> other)
        {
            return base.Equals(other);
        }
    }
}
