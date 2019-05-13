using System;
using System.Collections.Generic;
using System.Linq;
using AutomataLib;

namespace ContextFreeGrammar
{
    public class Production<TNonterminalSymbol> where TNonterminalSymbol : Symbol
    {
        public Production(TNonterminalSymbol head, IEnumerable<Symbol> tail)
        {
            Head = head ?? throw new ArgumentNullException(nameof(head));
            var rhs = (tail ?? Enumerable.Empty<Symbol>()).ToList();
            if (rhs.Count > 1 && rhs.Any(symbol => symbol.IsEpsilon))
                throw new ArgumentException($"{Symbol.Epsilon}-productions cannot have more than one symbol.");
            if (rhs.Count == 1 && rhs[0].IsEpsilon)
                Tail = new List<Symbol>();
            else
                Tail = rhs;
        }

        /// <summary>
        /// LHS
        /// </summary>
        public TNonterminalSymbol Head { get; }

        /// <summary>
        /// RHS list of grammar symbols (note that ε-production has empty Tail of Length zero).
        /// </summary>
        public IReadOnlyList<Symbol> Tail { get; }

        public TSymbol TailAs<TSymbol>(int i) where TSymbol : Symbol
        {
            return Tail[i] as TSymbol;
        }

        public int Length => Tail.Count;

        // IsEmpty == IsEpsilon
        public bool IsEpsilon => Tail.Count == 0;

        // IsNotEmpty == IsNotEpsilon
        public bool IsNotEpsilon => Tail.Count > 0;

        public Symbol FirstSymbol => Tail.Count > 0 ? Tail[0] : Symbol.Epsilon;

        public Symbol LastSymbol => Tail.Count > 0 ? Tail[Tail.Count - 1] : Symbol.Epsilon;

        public override string ToString()
        {
            return Tail.Count > 0
                ? $"{Head} → {string.Join(string.Empty, Tail.Select(symbol => symbol.Name))}"
                : $"{Head} → {Symbol.Epsilon}";
        }
    }
}
