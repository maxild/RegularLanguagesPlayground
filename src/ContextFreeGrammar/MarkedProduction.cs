using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using AutomataLib;
using JetBrains.Annotations;

namespace ContextFreeGrammar
{
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
    public struct MarkedProduction<TNonterminalSymbol> : IEquatable<MarkedProduction<TNonterminalSymbol>>
        where TNonterminalSymbol : Symbol, IEquatable<TNonterminalSymbol>
    {
        private string DebuggerDisplay => ToString();
        private const char DOT = '•'; // Bullet

        public MarkedProduction(
            Production<TNonterminalSymbol> production,
            int productionIndex,
            int markerPosition)
        {
            if (production == null)
            {
                throw new ArgumentNullException(nameof(production));
            }
            if (markerPosition > production.Length)
            {
                throw new ArgumentException("Invalid dot position --- The marker cannot be shifted beyond the end of the production.");
            }

            Production = production;
            ProductionIndex = productionIndex;
            MarkerPosition = markerPosition;
        }

        public Production<TNonterminalSymbol> Production { get; }

        public int ProductionIndex { get; }

        public int MarkerPosition { get; }

        /// <summary>
        /// Get the successor item of a shift/goto action created by 'shifting the dot'.
        /// </summary>
        [Pure]
        public MarkedProduction<TNonterminalSymbol> WithShiftedDot()
            => new MarkedProduction<TNonterminalSymbol>(Production, ProductionIndex, MarkerPosition + 1);

        /// <summary>
        /// Any item B → α•β where α is not ε (the empty string),
        /// or the start rule S' → •S item (of the augmented grammar that
        /// is the first production of index zero by convention). That is
        /// the initial item S' → •S, and all items where the dot is not at the left end.
        /// </summary>
        public bool IsCoreItem => MarkerPosition > 0 || ProductionIndex == 0;

        /// <summary>
        /// Is this item a completed item on the form A → α•, where the dot have been shifted
        /// all the way to the end of the production (a completed item is an accepting state,
        /// where we have recognized a handle)
        /// </summary>
        public bool IsReduceItem => MarkerPosition == Production.Tail.Count;

        /// <summary>
        /// B → α•Xβ (where X is a nonterminal symbol)
        /// </summary>
        public bool IsGotoItem => MarkerPosition < Production.Tail.Count && Production.Tail[MarkerPosition].IsNonTerminal;

        /// <summary>
        /// B → α•aβ (where a is a terminal symbol)
        /// </summary>
        public bool IsShiftItem => MarkerPosition < Production.Tail.Count && Production.Tail[MarkerPosition].IsTerminal;

        /// <summary>
        /// Get the symbol before the dot.
        /// </summary>
        [Pure]
        public Symbol GetPrevSymbol() => MarkerPosition > 0 ? Production.Tail[MarkerPosition - 1] : Symbol.Epsilon;

        /// <summary>
        /// Get the symbol after the dot.
        /// </summary>
        [Pure]
        public Symbol GetNextSymbol() => MarkerPosition < Production.Tail.Count ? Production.Tail[MarkerPosition] : Symbol.Epsilon;

        /// <summary>
        /// Get the symbol after the dot.
        /// </summary>
        [Pure]
        public TSymbol GetNextSymbol<TSymbol>() where TSymbol : Symbol
        {
            return (TSymbol) GetNextSymbol();
        }

        /// <summary>
        /// Get the symbol after the dot.
        /// </summary>
        [Pure]
        public TSymbol GetNextSymbolAs<TSymbol>() where TSymbol : Symbol
        {
            return GetNextSymbol() as TSymbol;
        }

        /// <summary>
        /// Get the remaining symbols after the dot.
        /// </summary>
        [Pure]
        public IEnumerable<Symbol> GetRemainingSymbolsAfterNextSymbol()
        {
            for (int i = MarkerPosition + 1; i < Production.Length; i++)
            {
                yield return Production.Tail[i];
            }
        }

        [Pure]
        public bool Equals(MarkedProduction<TNonterminalSymbol> other)
        {
            return ProductionIndex == other.ProductionIndex && MarkerPosition == other.MarkerPosition;
        }

        public override bool Equals(object obj)
        {
            return obj is MarkedProduction<TNonterminalSymbol> item && Equals(item);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = MarkerPosition;
                hashCode = (hashCode * 397) ^ ProductionIndex;
                return hashCode;
            }
        }

        public override string ToString()
        {
            MarkedProduction<TNonterminalSymbol> self = this;

            StringBuilder dottedTail = self.Production.Tail
                .Aggregate((i: 0, sb: new StringBuilder()),
                    (t, symbol) =>
                    {
                        if (t.i == self.MarkerPosition)
                        {
                            t.sb.Append(DOT);
                        }

                        return (i: t.i + 1, sb: t.sb.Append(symbol.Name));
                    }).sb;

            if (MarkerPosition == Production.Tail.Count)
            {
                dottedTail.Append(DOT);
            }

            return $"{Production.Head} → {dottedTail}";
        }
    }
}
