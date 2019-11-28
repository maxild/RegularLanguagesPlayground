using System;
using System.Collections.Generic;
using System.Linq;
using AutomataLib;

namespace ContextFreeGrammar
{
    public class Production<TNonterminalSymbol> where TNonterminalSymbol : Symbol
    {
        private readonly Symbol[] _tail;

        public Production(TNonterminalSymbol head, IEnumerable<Symbol> tail)
        {
            Head = head ?? throw new ArgumentNullException(nameof(head));
            var rhs = (tail ?? Enumerable.Empty<Symbol>()).ToArray();
            if (rhs.Length > 1 && rhs.Any(symbol => symbol.IsEpsilon))
                throw new ArgumentException($"{Symbol.Epsilon}-productions cannot have more than one symbol.");
            if (rhs.Length == 1 && rhs[0].IsEpsilon)
                _tail = Array.Empty<Symbol>();
            else
                _tail = rhs;
        }

        /// <summary>
        /// LHS
        /// </summary>
        public TNonterminalSymbol Head { get; }

        /// <summary>
        /// RHS list of grammar symbols (note that ε-production has empty Tail of Length zero).
        /// </summary>
        public IReadOnlyList<Symbol> Tail => _tail;

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

        /// <summary>
        /// Get the symbols after the marker position in normal order.
        /// </summary>
        public IEnumerable<Symbol> GetSymbolsAfterMarkerPosition(int markerPosition)
        {
            // slicing in c# 8
            //return _tail[(markerPosition+1)..];
            for (int i = markerPosition + 1; i < Tail.Count; i += 1)
                yield return Tail[i];
        }

        /// <summary>
        /// Get the symbols in reverse order before the marker position
        /// </summary>
        public IEnumerable<Symbol> GetSymbolsBeforeMarkerPosition(int markerPosition)
        {
            // slicing in c# 8
            //return _tail[..(markerPosition-1)];
            for (int i = markerPosition - 1; i >= 0; i -= 1)
                yield return Tail[i];
        }

        public override string ToString()
        {
            return Tail.Count > 0
                ? $"{Head} → {string.Join(string.Empty, Tail.Select(symbol => symbol.Name))}"
                : $"{Head} → {Symbol.Epsilon}";
        }
    }
}
