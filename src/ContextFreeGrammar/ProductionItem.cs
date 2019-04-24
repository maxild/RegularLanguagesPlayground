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
    public struct ProductionItem : IEquatable<ProductionItem>, IFiniteAutomatonState
    {
        private string DebuggerDisplay => ToString();
        private const char DOT = '•'; // Bullet

        private readonly Production _production;
        private readonly int _productionIndex;
        private readonly int _dotPosition;

        public ProductionItem(Production production, int productionIndex, int dotPosition)
        {
            if (dotPosition > production.Tail.Count)
            {
                throw new ArgumentException();
            }

            _production = production;
            _productionIndex = productionIndex;
            _dotPosition = dotPosition;
        }

        /// <summary>
        /// Any item B → α.β where α is not ε (the empty string),
        /// or the start rule S' -> .S item (of the augmented grammar that
        /// is the first production of index zero by convention).
        /// </summary>
        public bool IsCoreItem => _dotPosition > 0 || _productionIndex == 0;

        /// <summary>
        /// A → α. (accepting state)
        /// </summary>
        public bool IsReduceItem => _dotPosition == _production.Tail.Count;

        /// <summary>
        /// B → α.Xβ (X in V)
        /// </summary>
        public bool IsGotoItem => _dotPosition < _production.Tail.Count && _production.Tail[_dotPosition].IsNonTerminal;

        /// <summary>
        /// B → α.aβ (a in T)
        /// </summary>
        public bool IsShiftItem => _dotPosition < _production.Tail.Count && _production.Tail[_dotPosition].IsTerminal;

        public Symbol GetNextSymbol() => _dotPosition < _production.Tail.Count ? _production.Tail[_dotPosition] : null;

        public TSymbol GetNextSymbol<TSymbol>() where TSymbol : Symbol
        {
            return (TSymbol) GetNextSymbol();
        }

        public ProductionItem GetNextItem() => new ProductionItem(_production, _productionIndex, _dotPosition + 1);

        public bool Equals(ProductionItem other)
        {
            return _productionIndex == other._productionIndex && _dotPosition == other._dotPosition;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is ProductionItem)) return false;
            return Equals((ProductionItem) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (_productionIndex * 397) ^ _dotPosition;
            }
        }

        string IFiniteAutomatonState.Id => $"{_productionIndex}_{_dotPosition}";

        string IFiniteAutomatonState.Label => ToString();

        public override string ToString()
        {
            ProductionItem self = this;
            StringBuilder dottedTail = self._production.Tail
                .Aggregate((i: 0, sb: new StringBuilder()),
                    (t, symbol) =>
                    {
                        if (t.i == self._dotPosition)
                        {
                            t.sb.Append(DOT);
                        }
                        return (i: t.i + 1, sb: t.sb.Append(symbol.Name));
                    }).sb;

            if (_dotPosition == _production.Tail.Count)
            {
                dottedTail.Append(DOT);
            }

            return $"{_production.Head} → {dottedTail}";
        }
    }
}
