using System;
using System.Collections.Generic;
using System.Linq;

namespace ContextFreeGrammar
{
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
    public class NonTerminal : Symbol
    {
        internal NonTerminal(string name)
            : base(name)
        {
        }

        public override bool IsTerminal => false;

        public override bool IsEpsilon => false;
    }

    /// <summary>
    /// The single character terminal (non-essential simplification).
    /// </summary>
    public class Terminal : Symbol
    {
        /// <summary>
        /// We simplify things by assuming that the name (kind of lexical unit) of a terminal/token and
        /// an optional attribute value (lexeme, symbol table lookup etc) of any terminal/token are the
        /// same thing, and this thing is a single character.
        /// </summary>
        /// <param name="value">The single character name/value of the terminal.</param>
        internal Terminal(char value)
            : base(new string(value, 1))
        {
        }

        public override bool IsTerminal => true;

        public override bool IsEpsilon => false;
    }

    //public class GrammarVariables
    //{

    //}

    //public class GrammarTerminals<TToken>
    //{

    //}

}
