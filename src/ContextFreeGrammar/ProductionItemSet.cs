using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AutomataLib;

namespace ContextFreeGrammar
{
    /// <summary>
    /// A set of LR(0) items that together form a single state in the DFA of the so-called LR(0) automaton.
    /// This DFA is our so called "LR(0) viable prefix (handle) recognizer" used to construct
    /// the parser table of any shift/reduce LR parser. Note that all states of the DFA except the initial state
    /// satisfies the so-called spelling property that only a single label/symbol will move/transition into that state.
    /// Thus each state except the initial state has a unique grammar symbol associated with it.
    /// </summary>
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
    public class ProductionItemSet<TNonterminalSymbol> : IEquatable<ProductionItemSet<TNonterminalSymbol>> //, IReadOnlySet<ProductionItem> TODO: Do we need IReadOnlySet support?
        where TNonterminalSymbol : Symbol, IEquatable<TNonterminalSymbol>
    {
        private string DebuggerDisplay => ClosureItems.Any()
            ? string.Concat(CoreItems.ToVectorString(), ":", ClosureItems.ToVectorString())
            : CoreItems.ToVectorString();

        // core items are always non-empty (the core items identifies the LR(0) item set)
        private readonly HashSet<ProductionItem<TNonterminalSymbol>> _coreItems;
        // closure items can be empty (and can BTW always be generated on the fly, but we store them to begin with)
        private readonly List<ProductionItem<TNonterminalSymbol>> _closureItems;

        public ProductionItemSet(IEnumerable<ProductionItem<TNonterminalSymbol>> items)
        {
            _coreItems = new HashSet<ProductionItem<TNonterminalSymbol>>();
            _closureItems = new List<ProductionItem<TNonterminalSymbol>>();
            foreach (var item in items)
            {
                if (item.IsCoreItem)
                    _coreItems.Add(item);
                else
                    _closureItems.Add(item);
            }
        }

        /// <summary>
        /// Because all transitions entering any given state in the DFA for the LR(0) automaton have the same label,
        /// the LR(0) item set has a unique spelling property, that can be used to compute the sentential form
        /// during shift/reduce parsing.
        /// </summary>
        public Symbol SpellingSymbol => CoreItems.First().GetPrevSymbol(); // all core items have the same grammar symbol to the left of the dot

        public IEnumerable<ProductionItem<TNonterminalSymbol>> Items => _coreItems.Concat(_closureItems);

        /// <summary>
        /// The partially parsed rules for a state are called its core LR(0) items.
        /// If we also call S′ −→ .S a core item, we observe that every state in the
        /// DFA is completely determined by its subset of core items.
        /// </summary>
        public IEnumerable<ProductionItem<TNonterminalSymbol>> CoreItems => _coreItems;

        /// <summary>
        /// The closure items (obtained via ϵ-closure) do not determine the state of the LR(0) automaton,
        /// because they can all be forgotten about, and regenerated on the fly. All closure items have
        /// the dot at the beginning of the rule, and are therefore not parsed yet.
        /// </summary>
        public IEnumerable<ProductionItem<TNonterminalSymbol>> ClosureItems => _closureItems;

        /// <summary>
        /// Reduce items (not including the first production S' -> S of the augmented grammar)
        /// </summary>
        public IEnumerable<ProductionItem<TNonterminalSymbol>> ReduceItems => Items.Where(item => item.IsReduceItem);

        public bool IsAcceptAction
        {
            get
            {
                var coreItem = CoreItems.First();
                return coreItem.ProductionIndex == 0 && coreItem.IsReduceItem;
            }
        }

        public bool IsShiftAction { get; }

        public bool IsReduceAction { get; }

        /// <summary>
        /// Goto or shift items (this is the core items of the GOTO function in dragon book)
        /// </summary>
        public ILookup<Symbol, ProductionItem<TNonterminalSymbol>> GetTargetItems()
        {
            return Items
                .Where(item => !item.IsReduceItem)
                .ToLookup(item => item.GetNextSymbol(), item => item.GetNextItem());
        }

        public bool Equals(ProductionItemSet<TNonterminalSymbol> other)
        {
            return other != null && _coreItems.SetEquals(other.CoreItems);
        }

        public IEnumerator<ProductionItem<TNonterminalSymbol>> GetEnumerator()
        {
            return Items.GetEnumerator();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is ProductionItemSet<TNonterminalSymbol>)) return false;
            return Equals((ProductionItemSet<TNonterminalSymbol>) obj);
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
