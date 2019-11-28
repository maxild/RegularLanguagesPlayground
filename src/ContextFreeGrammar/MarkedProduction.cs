using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using AutomataLib;
using JetBrains.Annotations;

namespace ContextFreeGrammar
{
    /// <summary>
    /// CORE of an LR(k) item. That is the LR(0) item part, where the lookahead part is dropped.
    /// </summary>
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
            if (markerPosition < 0)
            {
                throw new ArgumentException("Invalid dot position --- The marker position cannot be less than zero.");
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


        public ProductionItem<TNonterminalSymbol, TTerminalSymbol> AsLr0Item<TTerminalSymbol>()
            where TTerminalSymbol : Symbol, IEquatable<TTerminalSymbol>
                => new ProductionItem<TNonterminalSymbol, TTerminalSymbol>(this, Set<TTerminalSymbol>.Empty);

        /// <summary>
        /// Any item B → α•β where α is not ε (the empty string),
        /// or the start S' → •S item (of the augmented grammar). That is
        /// the initial item S' → •S, and all other items, where the dot is not
        /// at the beginning of the RHS are considered kernel items.
        /// </summary>
        public bool IsKernelItem => MarkerPosition > 0 || ProductionIndex == 0;

        /// <summary>
        /// Any item A → •β where the dot is at the beginning of the RHS of the production,
        /// except the initial item S' → •S.
        /// </summary>
        public bool IsClosureItem => !IsKernelItem;

        /// <summary>
        /// Is this item a completed item (aka final item) on the form A → α•, where the dot have been shifted
        /// all the way to the end of the production (a completed item is an accepting state,
        /// where we have recognized a handle)
        /// </summary>
        public bool IsReduceItem => MarkerPosition == Production.Tail.Count; // DotSymbol == Symbol.Epsilon

        /// <summary>
        /// B → α•Xβ (where X is a nonterminal symbol)
        /// </summary>
        public bool IsGotoItem => DotSymbol.IsNonTerminal; // IsNonterminalTransition

        /// <summary>
        /// B → α•aβ (where a is a terminal symbol)
        /// </summary>
        public bool IsShiftItem => DotSymbol.IsTerminal; // IsTerminalTransition

        /// <summary>
        /// Get the symbol before the dot.
        /// </summary>
        [Pure]
        public Symbol GetPrevSymbol() => MarkerPosition > 0 ? Production.Tail[MarkerPosition - 1] : Symbol.Epsilon;

        /// <summary>
        /// All kernel items (of any item set) have the same symbol before the dot.
        /// If the item is a closure item the result is the empty symbol (ε).
        /// </summary>
        public Symbol SpellingSymbol => GetPrevSymbol();

        /// <summary>
        /// The symbol after the dot. If the dot have been shifted all the way to the end of the RHS of
        /// the production the result is the empty symbol (ε).
        /// </summary>
        public Symbol DotSymbol => MarkerPosition < Production.Tail.Count ? Production.Tail[MarkerPosition] : Symbol.Epsilon;

        /// <summary>
        /// Get the symbol after the dot.
        /// </summary>
        [Pure]
        public TSymbol GetDotSymbol<TSymbol>() where TSymbol : Symbol
        {
            return (TSymbol) DotSymbol;
        }

        /// <summary>
        /// Get the symbol after the dot.
        /// </summary>
        [Pure]
        public TSymbol TryGetDotSymbol<TSymbol>() where TSymbol : Symbol
        {
            return DotSymbol as TSymbol;
        }

        /// <summary>
        /// Get the remaining symbols after the dot symbol.
        /// </summary>
        [Pure]
        public IEnumerable<Symbol> GetRemainingSymbolsAfterDotSymbol()
        {
            return Production.GetSymbolsAfterMarkerPosition(MarkerPosition);
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
