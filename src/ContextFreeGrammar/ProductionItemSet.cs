using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AutomataLib;

namespace ContextFreeGrammar
{
    /// <summary>
    /// A set of LR(0) items that together form a ste in the DFA of the LR(0) automaton.
    /// This DFA is our so called "LR(0) viable prefix (handle) recognizer" used to construct
    /// the parser table of any shift/reduce LR parser.
    /// </summary>
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
    public class ProductionItemSet : IEquatable<ProductionItemSet>
    {
        private string DebuggerDisplay => ClosureItems.Any()
            ? string.Concat(CoreItems.ToVectorString(), ":", ClosureItems.ToVectorString())
            : CoreItems.ToVectorString();

        private readonly HashSet<ProductionItem> _coreItems;    // always non-empty (the core items identifies the LR(0) item set)
        private readonly List<ProductionItem> _closureItems;    // can be empty (can always be generated on the fly, but we calculate anyway)

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

        public IEnumerable<ProductionItem> Items => _coreItems.Concat(_closureItems);

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

        /// <summary>
        /// Reduce items
        /// </summary>
        public IEnumerable<ProductionItem> ReduceItems => Items.Where(item => item.IsReduceItem);

        /// <summary>
        /// Goto or shift items (this is the core items of the GOTO function in dragon book)
        /// </summary>
        public ILookup<Symbol, ProductionItem> GetTargetItems()
        {
            return Items
                .Where(item => !item.IsReduceItem)
                .ToLookup(item => item.GetNextSymbol(), item => item.GetNextItem());
        }

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
            return ClosureItems.Any()
                ? string.Concat(CoreItems.ToVectorString(), Environment.NewLine, ClosureItems.ToVectorString())
                : CoreItems.ToVectorString();
        }
    }
}
