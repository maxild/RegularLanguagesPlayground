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
    public abstract class Symbol : IEquatable<Symbol>
    {
        public static readonly Terminal Epsilon = new Eps();

        public static NonTerminal V(string name)
        {
            return new NonTerminal(name);
        }

        public static IEnumerable<NonTerminal> Vs(params string[] names)
        {
            return names.Select(name => new NonTerminal(name));
        }

        public static Terminal T(char name)
        {
            return new Terminal(name);
        }

        public static IEnumerable<Terminal> Ts(params char[] names)
        {
            return names.Select(name => new Terminal(name));
        }

        class Eps : Terminal
        {
            public Eps() : base('ε')
            {
            }

            /// <summary>
            /// Base case for nullable, but nullable is something to discover by solving
            /// recursive equations of set variables (fix point, discovery algorithm).
            /// </summary>
            /// <returns>true iff terminal symbol is nullable (e.g. empty string)</returns>
            public override bool IsEpsilon => true;
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

        public bool IsNonTerminal => !IsTerminal;

        public abstract bool IsEpsilon { get; }

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
    }

    /// <summary>
    /// A variable in V.
    /// </summary>
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
    public class NonTerminal : Symbol, IEquatable<NonTerminal>
    {
        private string DebuggerDisplay => Name;

        internal NonTerminal(string name)
            : base(name)
        {
        }

        public override bool IsTerminal => false;

        public override bool IsEpsilon => false;

        public bool Equals(NonTerminal other)
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

        public override bool IsEpsilon => false;

        public bool Equals(Terminal other)
        {
            return base.Equals(other);
        }
    }
}
