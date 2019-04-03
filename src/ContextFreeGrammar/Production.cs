using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ContextFreeGrammar
{
    public class Production
    {
        public Production(NonTerminal head, IEnumerable<Symbol> tail)
        {
            Head = head;
            Tail = tail.ToList();
        }

        /// <summary>
        /// LHS
        /// </summary>
        public NonTerminal Head { get; }

        /// <summary>
        /// RHS
        /// </summary>
        public List<Symbol> Tail { get; }

        public override string ToString()
        {
            return $"{Head} → {string.Join(string.Empty, Tail.Select(symbol => symbol.Name))}";
        }
    }

    /// <summary>
    /// Dotted production used in Donald Knuth's LR(0) Characteristic Strings Automata.
    /// DFA for the regular language containing the set of prefixes (of grammar symbols)
    /// of right-most sentential forms, where the prefix ends in a handle that can be reduced
    /// by some production rule.
    ///
    /// LR(0) (dotted) core item: every state is completely determined by its subset of core items
    /// </summary>
    public sealed class ProductionItem : IEquatable<ProductionItem>
    {
        // The canonical collection of sets of LR(0) items
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
        /// If we also call S′ −→ .S a core item, we observe that every state is completely determined
        /// by its subset of core items. The other items in the state are obtained via ϵ-closure.
        /// </summary>
        public bool IsCoreItem => _dotPosition > 0; // TODO: Er den noedvendig???

        /// <summary>
        /// A −→ α. (accepting state)
        /// </summary>
        public bool IsReduceItem => _dotPosition == _production.Tail.Count;

        /// <summary>
        /// B −→ α.Xβ (X in V)
        /// </summary>
        public bool IsGotoItem => _dotPosition < _production.Tail.Count && _production.Tail[_dotPosition].IsNonTerminal;

        /// <summary>
        /// B −→ α.aβ (a in T)
        /// </summary>
        public bool IsShiftItem => _dotPosition < _production.Tail.Count && _production.Tail[_dotPosition].IsTerminal;

        public bool Equals(ProductionItem other)
        {
            if (other == null) return false;
            if (ReferenceEquals(this, other)) return true;
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

        public override string ToString()
        {
            StringBuilder dottedTail = _production.Tail
                .Aggregate((i: 0, sb: new StringBuilder()),
                    (t, symbol) =>
                    {
                        if (t.i == _dotPosition)
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
