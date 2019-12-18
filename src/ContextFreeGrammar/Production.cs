using System;
using System.Collections.Generic;
using System.Linq;

namespace ContextFreeGrammar
{
    public class Production
    {
        public Production(Nonterminal head, IEnumerable<Symbol> tail)
        {
            Head = head ?? throw new ArgumentNullException(nameof(head));
            var rhs = (tail ?? Enumerable.Empty<Symbol>()).ToArray();
            if (rhs.Length > 1 && rhs.Any(symbol => symbol.IsEpsilon))
                throw new ArgumentException($"{Symbol.Epsilon}-productions cannot have more than one symbol.");
            if (rhs.Length == 0 || rhs.Length == 1 && rhs[0].IsEpsilon)
                Tail = Array.Empty<Symbol>();
            else
                Tail = rhs;
        }

        /// <summary>
        /// LHS
        /// </summary>
        public Nonterminal Head { get; }

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

        public Symbol LastSymbol => Tail.Count > 0 ? Tail[^1] : Symbol.Epsilon;

        /// <summary>
        /// Get the symbols after the marker position in normal order.
        /// </summary>
        public IEnumerable<Symbol> GetSymbolsAfterMarkerPosition(int markerPosition)
        {
            // TODO: slicing in c# 8 throws, but are allocation free
            //return _tail[(markerPosition+1)..];
            for (int i = markerPosition + 1; i < Tail.Count; i += 1)
                yield return Tail[i];
        }

        /// <summary>
        /// Get the symbols before the marker position in reverse order.
        /// </summary>
        public IEnumerable<Symbol> GetSymbolsBeforeMarkerPosition(int markerPosition)
        {
            // TODO: slicing in c# 8 throws, but are allocation free
            //return _tail[..(markerPosition-1)];
            for (int i = markerPosition - 1; i >= 0; i -= 1)
                yield return Tail[i];
        }

        public override string ToString()
        {
            return Tail.Count > 0
                ? $"{Head} → {string.Join(" ", Tail.Select(symbol => symbol.Name))}"
                : $"{Head} → {Symbol.Epsilon}";
        }
    }
}
