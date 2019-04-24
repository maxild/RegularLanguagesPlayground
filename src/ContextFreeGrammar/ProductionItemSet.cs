using System;
using System.Collections.Generic;
using System.Diagnostics;
using AutomataLib;

namespace ContextFreeGrammar
{
    /// <summary>
    /// A set of LR(0) items constructed via the subset construction algorithm. Each set of LR(0) items,
    /// constructed from the NFA of all possible items, will represent a single state in a DFA. This
    /// DFA is our so called "LR(0) viable prefix recognition machine".
    /// The DFA states (and transitions) can also be constructed in a more efficient single pass algorithm.
    /// </summary>
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
    public class ProductionItemSet : IEquatable<ProductionItemSet>
    {
        private string DebuggerDisplay => CoreItems.ToVectorString();

        private readonly HashSet<ProductionItem> _coreItems;
        private readonly List<ProductionItem> _closureItems;

        public ProductionItemSet(IEnumerable<ProductionItem> items)
        {
            _coreItems = new HashSet<ProductionItem>();
            _closureItems = new List<ProductionItem>();
            foreach (var item in items)
            {
                if (item.IsCoreItem)
                    _coreItems.Add(item);
                else
                    _closureItems.Add(item);
            }
        }

        /// <summary>
        /// The partially parsed rules for a state are called its core LR(0) items.
        /// If we also call S′ −→ .S a core item, we observe that every state in the
        /// DFA is completely determined by its subset of core items.
        /// </summary>
        public IEnumerable<ProductionItem> CoreItems => _coreItems;

        /// <summary>
        /// The closure items (obtained via ϵ-closure) do not determine the state of the LR(0) automaton,
        /// because they can all be forgotten about, and regenerated on the fly. All closure items have
        /// the dot at the beginning of the rule, and are therefore not parsed yet.
        /// </summary>
        public IEnumerable<ProductionItem> ClosureItems => _closureItems;

        public bool Equals(ProductionItemSet other)
        {
            return other != null && _coreItems.SetEquals(other.CoreItems);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is ProductionItemSet)) return false;
            return Equals((ProductionItemSet) obj);
        }

        public override int GetHashCode()
        {
            int hashCode = 17;
            foreach (var item in CoreItems)
                hashCode = 31 * hashCode + item.GetHashCode();
            return hashCode;
        }

        public override string ToString()
        {
            return string.Concat(CoreItems.ToVectorString(), Environment.NewLine, ClosureItems.ToVectorString());
        }
    }
}
