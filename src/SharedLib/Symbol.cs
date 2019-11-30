using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace AutomataLib
{
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
        /// The name of the variable in the BNF (non-terminal), or the name of the token (i.e. name of some abstract
        /// input symbol, aka lexical unit, identified by the lexer). Both interpretations of name (for variable or terminal)
        /// are what the parser processes during derivations/reductions of the grammar.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Is the symbol a terminal symbol that is either part of language described by the grammar or
        /// is it the reserved <see cref="EofMarker"/> symbol. In other words, is the symbol part of the
        /// input language of the parser.
        /// </summary>
        /// <remarks>
        /// In textbooks the extended terminal symbols is defined by the set T' = T ∪ {$}, that contain
        /// all terminal symbols and the reserved eof marker symbol.
        /// If <see cref="IsExtendedTerminal"/> is equal to <c>true</c>, then we can safely downcast the symbol to
        /// the <see cref="Terminal"/> subclass. That is all <see cref="Terminal"/> derived symbols have
        /// <see cref="IsExtendedTerminal"/> equal to <c>true</c>.
        /// </remarks>
        public abstract bool IsExtendedTerminal { get; }

        /// <summary>
        /// Is the symbol a terminal symbol that is part of language described by the grammar.
        /// </summary>
        /// <remarks>
        /// All <see cref="Terminal"/> derived symbols have <see cref="IsTerminal"/> equal to <c>true</c>.
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
        /// Reserved symbol for the empty string.
        /// </summary>
        public static readonly Symbol Epsilon = new Eps();

        /// <summary>
        /// Reserved (terminal) symbol for end of input.
        /// </summary>
        /// <remarks>
        /// Many texts on parsing and compiling will not agree that the eof marker ($) is a terminal symbol.
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
        public static readonly Terminal EofMarker = new EndOfFileMarker();

        public static TTerminalSymbol Eof<TTerminalSymbol>()
        {
            Type t = typeof(TTerminalSymbol);
            if (t == typeof(Terminal))
            {
                return (TTerminalSymbol)(object)EofMarker;
            }
            throw new NotSupportedException($"A letterizer for the type '{t.Name}' is not supported.");
        }

        public static Nonterminal V(string name)
        {
            return new Nonterminal(name);
        }

        public static IEnumerable<Nonterminal> Vs(params string[] names)
        {
            return names.Select(name => new Nonterminal(name));
        }

        public static Terminal T(char name)
        {
            return new Terminal(name);
        }

        public static IEnumerable<Terminal> Ts(params char[] names)
        {
            return names.Select(name => new Terminal(name));
        }

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

        class EndOfFileMarker : Terminal
        {
            public EndOfFileMarker() : base('$')
            {
            }

            public override bool IsTerminal => false;

            public override bool IsEof => true;
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
    public class Terminal : Symbol, IEquatable<Terminal>
    {
        private string DebuggerDisplay => Name;

        /// <summary>
        /// We simplify things by assuming that the name (kind of lexical unit) of a terminal/token and
        /// an optional attribute value (lexeme, symbol table lookup etc) of any terminal/token are the
        /// same thing, and this thing is a single character.
        /// </summary>
        /// <param name="name">The single character name of the terminal.</param>
        internal Terminal(char name)
            : base(new string(name, 1))
        {
        }

        public override bool IsExtendedTerminal => true;

        public override bool IsTerminal => true;

        public override bool IsNonterminal => false;

        public override bool IsEpsilon => false;

        public override bool IsEof => false;

        public bool Equals(Terminal other)
        {
            return base.Equals(other);
        }
    }
}
