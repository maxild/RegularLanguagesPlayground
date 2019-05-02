using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using AutomataLib;

namespace ContextFreeGrammar
{
    /// <summary>
    /// Dotted production used in Donald Knuth's LR(0) Characteristic Strings Automata.
    /// DFA for the regular language containing the set of prefixes (of grammar symbols)
    /// of right-most sentential forms, where the prefix ends in a handle that can be reduced
    /// by some production rule.
    ///
    /// LR(0) (dotted) core item: every state is completely determined by its subset of core items
    ///
    /// The "core" of an LR item. This includes a production and the position
    /// of a marker (the "dot") within the production. Typically item cores
    /// are written using a production with an embedded "dot" to indicate their
    /// position: B → α"."β
    /// This represents a point in a parse where the parser is trying to match
    /// the given production, and has succeeded in matching everything before the
    /// "dot" (and hence is expecting to see the symbols after the dot next).
    /// </summary>
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
    public struct ProductionItem<TNonterminalSymbol> : IEquatable<ProductionItem<TNonterminalSymbol>>, IFiniteAutomatonState
        where TNonterminalSymbol : Symbol, IEquatable<TNonterminalSymbol>
    {
        private string DebuggerDisplay => ToString();
        private const char DOT = '•'; // Bullet

        private readonly int _dotPosition;

        public ProductionItem(Production<TNonterminalSymbol> production, int productionIndex, int dotPosition)
        {
            if (dotPosition > production.Tail.Count)
            {
                throw new ArgumentException();
            }

            Production = production;
            ProductionIndex = productionIndex;
            _dotPosition = dotPosition;
        }

        public Production<TNonterminalSymbol> Production { get; }

        public int ProductionIndex { get; }

        /// <summary>
        /// Any item B → α.β where α is not ε (the empty string),
        /// or the start rule S' -> .S item (of the augmented grammar that
        /// is the first production of index zero by convention). That is
        /// the initial item S' -> .S, and all items where the dot is not at the left end.
        /// </summary>
        public bool IsCoreItem => _dotPosition > 0 || ProductionIndex == 0;

        /// <summary>
        /// Any item A → α. where the dot is at the right end (accepting state, where
        /// we have recognized a viable prefix, and therefore a handle)
        /// </summary>
        public bool IsReduceItem => _dotPosition == Production.Tail.Count;

        /// <summary>
        /// B → α.Xβ (X is a nonterminal symbol)
        /// </summary>
        public bool IsGotoItem => _dotPosition < Production.Tail.Count && Production.Tail[_dotPosition].IsNonTerminal;

        /// <summary>
        /// B → α.aβ (a is a terminal symbol)
        /// </summary>
        public bool IsShiftItem => _dotPosition < Production.Tail.Count && Production.Tail[_dotPosition].IsTerminal;

        /// <summary>
        /// Get the symbol before the dot.
        /// </summary>
        public Symbol GetPrevSymbol() => _dotPosition > 0 ? Production.Tail[_dotPosition - 1] : null;

        /// <summary>
        /// Get the symbol after the dot.
        /// </summary>
        public Symbol GetNextSymbol() => _dotPosition < Production.Tail.Count ? Production.Tail[_dotPosition] : null;

        public TSymbol GetNextSymbol<TSymbol>() where TSymbol : Symbol
        {
            return (TSymbol) GetNextSymbol();
        }

        public TSymbol GetNextSymbolAs<TSymbol>() where TSymbol : Symbol
        {
            return GetNextSymbol() as TSymbol;
        }

        public ProductionItem<TNonterminalSymbol> GetNextItem() =>
            new ProductionItem<TNonterminalSymbol>(Production, ProductionIndex, _dotPosition + 1);

        public bool Equals(ProductionItem<TNonterminalSymbol> other)
        {
            return ProductionIndex == other.ProductionIndex && _dotPosition == other._dotPosition;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is ProductionItem<TNonterminalSymbol>)) return false;
            return Equals((ProductionItem<TNonterminalSymbol>) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (ProductionIndex * 397) ^ _dotPosition;
            }
        }

        string IFiniteAutomatonState.Id => $"{ProductionIndex}_{_dotPosition}";

        string IFiniteAutomatonState.Label => ToString();

        public override string ToString()
        {
            ProductionItem<TNonterminalSymbol> self = this;
            StringBuilder dottedTail = self.Production.Tail
                .Aggregate((i: 0, sb: new StringBuilder()),
                    (t, symbol) =>
                    {
                        if (t.i == self._dotPosition)
                        {
                            t.sb.Append(DOT);
                        }
                        return (i: t.i + 1, sb: t.sb.Append(symbol.Name));
                    }).sb;

            if (_dotPosition == Production.Tail.Count)
            {
                dottedTail.Append(DOT);
            }

            return $"{Production.Head} → {dottedTail}";
        }
    }
}
