using System;
using System.Collections.Generic;
using System.Linq;
using AutomataLib;

namespace ContextFreeGrammar
{
    public class Production
    {
        public Production(NonTerminal head, IEnumerable<Symbol> tail)
        {
            Head = head ?? throw new ArgumentNullException(nameof(head));
            var rhs = (tail ?? Enumerable.Empty<Symbol>()).ToList();
            if (rhs.Count > 1 && rhs.Any(symbol => symbol.IsEpsilon))
                throw new ArgumentException($"{Symbol.Epsilon}-productions cannot have more than one symbol.");
            Tail = rhs.Count > 0 ? rhs : new List<Symbol>(1) { Symbol.Epsilon };
        }

        /// <summary>
        /// LHS
        /// </summary>
        public NonTerminal Head { get; }

        /// <summary>
        /// RHS list of grammar symbols (epsilon, terminal symbols and nonterminal symbols).
        /// </summary>
        public IReadOnlyList<Symbol> Tail { get; } // TODO: Epsilon could be empty Tail

        public TSymbol TailAs<TSymbol>(int i) where TSymbol : Symbol
        {
            return Tail[i] as TSymbol;
        }

        public int Length => Tail.Count;

        public bool IsEpsilon => Tail.Count == 1 && Tail[0].IsEpsilon;

        public bool IsNotEpsilon => !IsEpsilon;

        public Symbol FirstSymbol => Tail[0];

        public Symbol LastSymbol => Tail[Tail.Count - 1];

        public override string ToString()
        {
            return $"{Head} → {string.Join(string.Empty, Tail.Select(symbol => symbol.Name))}";
        }
    }
}
