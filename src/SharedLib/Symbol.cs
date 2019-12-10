using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace AutomataLib
{
    // Conventions (to be validated by grammar type/class):
    //   tokens have to represented by enumeration indexed 0,1,2...,N (table indexes)
    //   terminals are uppercase words: NUM, ID etc...
    //   variables have to be represented by enumeration 0,1,2,...N (table indexes)
    //   nonterminals are lowercase word: expression, term, factor, declaration
    //   terminals are uniquely indexed by enum (maybe generated from grammar spec). EOF and EPS are both tokens.
    //   nonterminals are insertion ordered by their definition in grammar.
    //
    //                       IsExtendedTerminal    IsTerminal    IsNonterminal    IsEpsilon      IsEof
    //       EOF                    true              false          false           false       true
    //       EPS                    true              true           false           true        false
    //       ID/NUM                 true              true           false           false       false
    //       expr/factor            false             false          true            false       false

    // NOTE: Type (int, Node, string) etc is not part yet of grammar (no semantic actions yet)

    // TODO: After supporting BNF (written a parser) we should really soften the requirements that
    // Variables are uppercase letters and terminals are a single lowercase letter

    // TODO: We really need a Lexer!!!!! that generate stream of <num, "123">, <id, "x">,...etc without using symboltable
    //            var t = new Lexer(LexerSpecification spec, string content);
    //            Terminal token = t.GetNext(); // <name, value>
    //            string name = token.Name;
    //            string value = token.Lexeme;

    // NOTE: Name of terminal symbols are the only interesting terminal-attribute when building the parser. However
    //       when using the parser to accept/recognize a string, or to build a parse-tree we need a Lexer of some kind.

    /// <summary>
    /// Grammar symbol (in T or V)
    /// </summary>
    public abstract class Symbol : IEquatable<Symbol>, IComparable<Symbol>
    {
        /// <summary>
        /// The name of the (grammar) variable (nonterminal) in the BNF, or the name of the terminal symbol (i.e. the name of some abstract
        /// input symbol, aka token kind, lexical unit, identified by the lexer). Both interpretations of name (for variable or terminal)
        /// are what the parser processes during derivations/reductions of the grammar.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Is the symbol a terminal symbol that is either part of language described by the grammar or
        /// is it the reserved EOF symbol. In other words, is the symbol part of the
        /// input language of the parser.
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
        /// Is the symbol a terminal symbol that is part of language described by the grammar.
        /// </summary>
        /// <remarks>
        /// All <see cref="Terminal{TTokenKind}"/> derived symbols have <see cref="IsTerminal"/> equal to <c>true</c>.
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
        public static Terminal<TTokenKind> Eof<TTokenKind>() where TTokenKind : Enum => Terminal<TTokenKind>.EOF;

        /// <summary>
        /// Reserved symbol for the empty string.
        /// </summary>
        public static readonly Symbol Epsilon = new Eps();

        class Eps : Symbol
        {
            public Eps() : base(new string('ε', 1))
            {
            }

            public override bool IsExtendedTerminal => false;

            public override bool IsTerminal => false;

            public override bool IsNonterminal => false;

            public override bool IsEpsilon => true;

            public override bool IsEof => false;
        }

        public static Nonterminal V(string name)
        {
            return new Nonterminal(name);
        }

        public static IEnumerable<Nonterminal> Vs(params string[] names)
        {
            return names.Select(name => new Nonterminal(name));
        }

        public static Terminal<TTokenKind> T<TTokenKind>(TTokenKind kind) where TTokenKind : Enum
        {
            return Terminal<TTokenKind>.T(kind);
        }

        public static IEnumerable<Terminal<TTokenKind>> Ts<TTokenKind>(params TTokenKind[] kinds) where TTokenKind : Enum
        {
            return kinds.Select(Terminal<TTokenKind>.T);
        }

        protected Symbol(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(name);
            }
            Name = name;
        }

        public override string ToString()
        {
            return Name;
        }

        public bool Equals(Symbol other)
        {
            if (other == null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (GetType() != other.GetType())
            {
                return false;
            }

            // TODO: Use Index!!!!
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
            return Name.GetHashCode();
        }

        // TODO: Why compare (Ord)????
        public int CompareTo(Symbol other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            return string.Compare(Name, other.Name, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// A variable in V.
    /// </summary>
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
    public class Nonterminal : Symbol, IEquatable<Nonterminal>
    {
        private string DebuggerDisplay => Name;

        internal Nonterminal(string name)
            : base(name)
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
    /// The single character terminal (non-essential simplification).
    /// </summary>
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class Terminal<TTokenKind> : Symbol, IEquatable<Terminal<TTokenKind>>
        where TTokenKind : Enum
    {
        /// <summary>
        /// Reserved terminal grammar symbol used in the initial augmented unit production.
        /// </summary>
        internal static readonly Terminal<TTokenKind> EOF = new Terminal<TTokenKind>((TTokenKind)Enum.Parse(typeof(TTokenKind), "EOF"));

        public static readonly Terminal<TTokenKind>[] s_terminals;

        // NOTE: the static constructor will be called once for each closed class type (i.e. once for each token kind)
        static Terminal()
        {
            s_terminals = Enum.GetValues(typeof(TTokenKind)).Cast<TTokenKind>().Select(kind => new Terminal<TTokenKind>(kind)).ToArray();
        }

        internal static Terminal<TTokenKind> T(TTokenKind kind)
        {
            return s_terminals[Convert.ToInt32(kind)];
        }

        private string DebuggerDisplay => Name;

        /// <summary>
        /// We simplify things by assuming that the name (token kind of the lexical unit) of a terminal is all we care about.
        /// </summary>
        /// <param name="kind">The kind of token (enum value).</param>
        internal Terminal(TTokenKind kind)
            : base(Enum.GetName(typeof(TTokenKind), kind))
        {
            Kind = kind;
            Index = Convert.ToInt32(kind);
        }

        public TTokenKind Kind { get; }

        /// <summary>
        /// The raw value of the token kind.
        /// </summary>
        public int Index { get; }

        public override bool IsExtendedTerminal => true;

        public override bool IsTerminal => !IsEof; // By convention EOF is not a terminal

        public override bool IsNonterminal => false;

        public override bool IsEpsilon => false;

        public override bool IsEof => Equals(EOF);

        public bool Equals(Terminal<TTokenKind> other)
        {
            return other != null && EqualityComparer<TTokenKind>.Default.Equals(Kind, other.Kind);
        }
    }
}
