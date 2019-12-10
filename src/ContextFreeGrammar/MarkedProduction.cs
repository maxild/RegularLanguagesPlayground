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
    public struct MarkedProduction : IEquatable<MarkedProduction>
    {
        private string DebuggerDisplay => ToString();
        private const char DOT = '•'; // Bullet

        public MarkedProduction(
            Production production,
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

        public Production Production { get; }

        public int ProductionIndex { get; }

        public int MarkerPosition { get; }

        /// <summary>
        /// Get the successor item of a shift/goto action created by 'shifting the dot'.
        /// </summary>
        [Pure]
        public MarkedProduction WithShiftedDot()
            => new MarkedProduction(Production, ProductionIndex, MarkerPosition + 1);


        public ProductionItem<TTokenKind> AsLr0Item<TTokenKind>() where TTokenKind : Enum
            => new ProductionItem<TTokenKind>(this, Set<Terminal<TTokenKind>>.Empty);

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
        /// where we have recognized a handle). The item S' → S•$ is also a final item, because we want the states
        /// of the LR(0) automaton to be (numbered) the same way for both S' → S and S' → S$.
        /// </summary>
        ///  <remarks>
        /// For any reduce item the <see cref="DotSymbol"/> will be either <see cref="Symbol.Epsilon"/> or <see cref="Symbol.Eof{TTokenKind}"/>.
        /// For all 'semantic' reductions the <see cref="DotSymbol"/> will be <see cref="Symbol.Epsilon"/>, and for the augmented reduce item
        /// the <see cref="DotSymbol"/> will be <see cref="Symbol.Eof{TTokenKind}"/> by convention. Therefore one cannot use <see cref="DotSymbol"/>
        /// to test for the final item property, and all such tests should be based on <see cref="IsReduceItem"/>.
        /// </remarks>
        public bool IsReduceItem => MarkerPosition == Production.Tail.Count ||
                                    // S' → S•$ is the only final item, where the dot have not been shifted all the way to the end
                                    ProductionIndex == 0 && Production.Tail[^1].IsEof && MarkerPosition == Production.Tail.Count - 1;

        /// <summary>
        /// B → α•Xβ (where X is a nonterminal symbol).
        /// </summary>
        public bool IsGotoItem => DotSymbol.IsNonterminal;

        /// <summary>
        /// B → α•aβ (where a is a terminal symbol -- that is not the <see cref="Symbol.Eof{TTokenKind}"/> symbol).
        /// </summary>
        public bool IsShiftItem => DotSymbol.IsTerminal;

        /// <summary>
        /// Get the symbol before the dot.
        /// </summary>
        /// <remarks>
        /// All kernel items (of any item set) have the same symbol before the dot.
        /// If the item is a closure item, or the initial kernel item S' → •S (S' → •S$),
        /// the result is the empty symbol <see cref="Symbol.Epsilon"/>.
        /// </remarks>
        public Symbol BeforeDotSpellingSymbol => MarkerPosition > 0 ? Production.Tail[MarkerPosition - 1] : Symbol.Epsilon;

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
        /// Get the remaining symbols before the dot symbol in reverse order.
        /// </summary>
        [Pure]
        public IEnumerable<Symbol> GetRemainingSymbolsBeforeDotSymbol() => Production.GetSymbolsBeforeMarkerPosition(MarkerPosition);

        /// <summary>
        /// Get the remaining symbols after the dot symbol in normal order.
        /// </summary>
        [Pure]
        public IEnumerable<Symbol> GetRemainingSymbolsAfterDotSymbol() => Production.GetSymbolsAfterMarkerPosition(MarkerPosition);

        [Pure]
        public bool Equals(MarkedProduction other)
        {
            return ProductionIndex == other.ProductionIndex && MarkerPosition == other.MarkerPosition;
        }

        public override bool Equals(object obj)
        {
            return obj is MarkedProduction item && Equals(item);
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
            MarkedProduction self = this;

            StringBuilder dottedTail = self.Production.Tail
                .Aggregate((i: 0, sb: new StringBuilder()),
                    (t, symbol) =>
                    {
                        if (t.i == self.MarkerPosition)
                        {
                            t.sb.Append(DOT);
                        }
                        else if (t.i > 0)
                        {
                            t.sb.Append(" ");
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
