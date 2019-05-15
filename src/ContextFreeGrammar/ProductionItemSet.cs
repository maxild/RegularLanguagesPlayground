using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AutomataLib;

namespace ContextFreeGrammar
{
    /// <summary>
    /// A set of LR(0) items that together form a single state in the DFA of LR(0) automaton.
    /// This DFA is our so called "LR(0) viable prefix (handle) recognizer" used to construct
    /// the parser table of any shift/reduce LR parser. Note that all states of the DFA except the initial state
    /// satisfies the so-called spelling property that only a single label/symbol will move/transition into that state.
    /// Thus each state except the initial state has a unique grammar symbol associated with it.
    /// </summary>
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
    public class ProductionItemSet<TNonterminalSymbol, TTerminalSymbol> : IEquatable<ProductionItemSet<TNonterminalSymbol, TTerminalSymbol>>
        //, IReadOnlySet<ProductionItem> TODO: Do we need IReadOnlySet support?
        where TNonterminalSymbol : Symbol, IEquatable<TNonterminalSymbol>
        where TTerminalSymbol : Symbol, IEquatable<TTerminalSymbol>
    {
        private string DebuggerDisplay => ClosureItems.Any()
            ? string.Concat(CoreItems.ToVectorString(), ":", ClosureItems.ToVectorString())
            : CoreItems.ToVectorString();

        // core items are always non-empty (the core items identifies the LR(0) item set)
        private readonly HashSet<ProductionItem<TNonterminalSymbol, TTerminalSymbol>> _coreItems;
        // closure items can be empty (and can BTW always be generated on the fly, but we store them to begin with)
        private readonly List<ProductionItem<TNonterminalSymbol, TTerminalSymbol>> _closureItems;

        public ProductionItemSet(IEnumerable<ProductionItem<TNonterminalSymbol, TTerminalSymbol>> items)
        {
            _coreItems = new HashSet<ProductionItem<TNonterminalSymbol, TTerminalSymbol>>();
            _closureItems = new List<ProductionItem<TNonterminalSymbol, TTerminalSymbol>>();
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

        public IEnumerable<ProductionItem<TNonterminalSymbol, TTerminalSymbol>> Items => _coreItems.Concat(_closureItems);

        /// <summary>
        /// The partially parsed rules for a state are called its core LR(0) items.
        /// If we also call S′ → .S a core item, we observe that every state in the
        /// DFA is completely determined by its subset of core items.
        /// </summary>
        public IEnumerable<ProductionItem<TNonterminalSymbol, TTerminalSymbol>> CoreItems => _coreItems;

        /// <summary>
        /// The closure items (obtained via ϵ-closure) do not determine the state of the LR(0) automaton,
        /// because they can all be forgotten about, and regenerated on the fly. All closure items have
        /// the dot at the beginning of the rule, and are therefore not parsed yet.
        /// </summary>
        public IEnumerable<ProductionItem<TNonterminalSymbol, TTerminalSymbol>> ClosureItems => _closureItems;

        /// <summary>
        /// Reduce items (not including the first production S' → S of the augmented grammar). If grammar has no
        /// ε-productions, then all (completed) reduce items are core items, but the single item of an ε-production
        /// is both a reduce item and and a closure item (it can never be a core item).
        /// </summary>
        public IEnumerable<ProductionItem<TNonterminalSymbol, TTerminalSymbol>> ReduceItems => Items.Where(item => item.IsReduceItem);

        /// <summary>
        /// Does this the item set contain the the augmented reduce item (S' → S•)?
        /// </summary>
        public bool IsAcceptAction => ReduceItems.Any(item => item.ProductionIndex == 0);

        /// <summary>
        /// Does this item set contain any reduce items (not including the augmented reduce item 'S' → S•')?
        /// </summary>
        public bool IsReduceAction => ReduceItems.Any(item => item.ProductionIndex > 0);

        /// <summary>
        /// Compute the successor goto items (for non-terminal label/symbol) and/or shift items
        /// (for terminal label/symbol). This is the core items of the GOTO function in the dragon book.
        /// </summary>
        public ILookup<Symbol, ProductionItem<TNonterminalSymbol, TTerminalSymbol>> GetTargetItems()
        {
            return Items
                .Where(item => !item.IsReduceItem)
                .ToLookup(item => item.GetNextSymbol(), item => item.WithShiftedDot());
        }

        public bool Equals(ProductionItemSet<TNonterminalSymbol, TTerminalSymbol> other)
        {
            return Equals(other, ProductionItemComparison.MarkedProductionAndLookaheads);
        }

        public bool Equals(ProductionItemSet<TNonterminalSymbol, TTerminalSymbol> other, ProductionItemComparison comparison)
        {
            if (other == null) return false;
            switch (comparison)
            {
                case ProductionItemComparison.MarkedProductionOnly:
                    return AsLr0CoreItems().SetEquals(other.AsLr0CoreItems());
                case ProductionItemComparison.LookaheadsOnly:
                    throw new InvalidOperationException("LR(k) item sets cannot be tested for equal lookahead sets only.");
                case ProductionItemComparison.MarkedProductionAndLookaheads:
                default:
                    return _coreItems.SetEquals(other.CoreItems);
            }
        }

        private HashSet<MarkedProduction<TNonterminalSymbol>> AsLr0CoreItems()
        {
            return new HashSet<MarkedProduction<TNonterminalSymbol>>(_coreItems.Select(item => item.MarkedProduction));
        }

        public IEnumerator<ProductionItem<TNonterminalSymbol, TTerminalSymbol>> GetEnumerator()
        {
            return Items.GetEnumerator();
        }

        public override bool Equals(object obj)
        {
            return obj is ProductionItemSet<TNonterminalSymbol, TTerminalSymbol> set && Equals(set);
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
