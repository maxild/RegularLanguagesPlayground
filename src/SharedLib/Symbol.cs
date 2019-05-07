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
        public static readonly Symbol Epsilon = new Eps();

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

            public override bool IsTerminal => false;

            public override bool IsNonTerminal => false;

            /// <summary>
            /// Base case for nullable, but nullable is something to discover by solving
            /// recursive equations of set variables (fix point iteration, discovery algorithm).
            /// </summary>
            /// <returns>true iff terminal symbol is nullable (e.g. empty string)</returns>
            public override bool IsEpsilon => true;

            public override bool IsEof => false;
        }

        class EndOfFileMarker : Terminal
        {
            public EndOfFileMarker() : base('$')
            {
            }

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

        /// <summary>
        /// The name of the variable in the BNF (non-terminal), or the name of the token (i.e. name of some abstract
        /// input symbol, aka lexical unit, identified by the lexer). Both interpretations of name (for variable or terminal)
        /// are what the parser processes during derivations/reductions of the grammar.
        /// </summary>
        public string Name { get; }

        public abstract bool IsTerminal { get;}

        public abstract bool IsNonTerminal { get; }

        public abstract bool IsEpsilon { get; }

        public abstract bool IsEof { get; }

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

        public override bool IsTerminal => false;

        public override bool IsNonTerminal => true;

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

        public override bool IsTerminal => true;

        public override bool IsNonTerminal => false;

        public override bool IsEpsilon => false;

        public override bool IsEof => false;

        public bool Equals(Terminal other)
        {
            return base.Equals(other);
        }
    }
}
