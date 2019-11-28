using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AutomataLib;

namespace ContextFreeGrammar
{
    /// <summary>
    /// A set of LR(k) items that together form a single state in the DFA of LR(k) automaton, where k=0 or k=1.
    /// LR(0) items form the basis of LR(0), SLR(1) and LALR(1) parsers, and LR(1) form the basis of CLR(1) (Canonical LR) parsers.
    /// This DFA is our so called "LR(k) viable prefix (handle) recognizer" used to construct
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
            ? string.Concat(KernelItems.ToVectorString(), ":", ClosureItems.ToVectorString())
            : KernelItems.ToVectorString();

        // kernel items are always non-empty (the kernel items identifies the LR(0) item set)
        private readonly HashSet<ProductionItem<TNonterminalSymbol, TTerminalSymbol>> _kernelItems;
        // closure items can be empty (and can BTW always be generated on the fly, but we store them to begin with)
        private readonly List<ProductionItem<TNonterminalSymbol, TTerminalSymbol>> _closureItems;

        public ProductionItemSet(IEnumerable<ProductionItem<TNonterminalSymbol, TTerminalSymbol>> items)
        {
            _kernelItems = new HashSet<ProductionItem<TNonterminalSymbol, TTerminalSymbol>>();
            _closureItems = new List<ProductionItem<TNonterminalSymbol, TTerminalSymbol>>();
            foreach (var item in items)
            {
                if (item.IsKernelItem)
                    _kernelItems.Add(item);
                else
                    _closureItems.Add(item);
            }
        }

        /// <summary>
        /// Does this LR(k) item set contain a kernel item with a given CORE (dotted production).
        /// </summary>
        public bool ContainsKernelItem(MarkedProduction<TNonterminalSymbol> dottedProduction)
        {
            return KernelItems.Select(item => item.MarkedProduction).Contains(dottedProduction);
        }

        /// <summary>
        /// Because all transitions entering any given state in the DFA for the LR(0) automaton have the same label,
        /// the LR(0) item set has a unique spelling property, that can be used to compute the sentential form
        /// during shift/reduce parsing.
        /// </summary>
        public Symbol SpellingSymbol => KernelItems.First().SpellingSymbol; // all kernel items have the same grammar symbol to the left of the dot

        public IEnumerable<ProductionItem<TNonterminalSymbol, TTerminalSymbol>> Items => _kernelItems.Concat(_closureItems);

        /// <summary>
        /// The partially parsed rules for a state are called its kernel LR(0) items.
        /// If we also call S′ → .S a kernel item, we observe that every state in the
        /// DFA is completely determined by its subset of kernel items.
        /// </summary>
        public IEnumerable<ProductionItem<TNonterminalSymbol, TTerminalSymbol>> KernelItems => _kernelItems;

        /// <summary>
        /// The closure items (obtained via ϵ-closure) do not determine the state of the LR(0) automaton,
        /// because they can all be forgotten about, and regenerated on the fly. All closure items have
        /// the dot at the beginning of the rule, and are therefore not parsed yet.
        /// </summary>
        public IEnumerable<ProductionItem<TNonterminalSymbol, TTerminalSymbol>> ClosureItems => _closureItems;

        /// <summary>
        /// Reduce items (not including the first production S' → S of the augmented grammar). If grammar has no
        /// ε-productions, then all (completed) reduce items are kernel items, but the single item of an ε-production
        /// is both a reduce item and and a closure item (it can never be a kernel item).
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
        /// Find all nonterminal labeled transitions out of this item set.
        /// </summary>
        public IEnumerable<TNonterminalSymbol> NonterminalTransitions =>
            Items.Where(item => item.DotSymbol.IsNonTerminal).Select(item => item.GetDotSymbol<TNonterminalSymbol>());

        /// <summary>
        /// Find all terminal labeled transitions out of this item set.
        /// </summary>
        public IEnumerable<TTerminalSymbol> TerminalTransitions =>
            Items.Where(item => item.DotSymbol.IsTerminal).Select(item => item.GetDotSymbol<TTerminalSymbol>());

        /// <summary>
        /// Compute the successor goto items (i.e for non-terminal transitions) and/or shift items
        /// (i.e. for terminal transitions). This is the kernel items of the GOTO function in the dragon book.
        /// </summary>
        public ILookup<Symbol, ProductionItem<TNonterminalSymbol, TTerminalSymbol>> GetTargetItems()
        {
            return Items
                .Where(item => !item.IsReduceItem)
                .ToLookup(item => item.DotSymbol, item => item.WithShiftedDot());
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
                    return AsLr0KernelItems().SetEquals(other.AsLr0KernelItems());
                case ProductionItemComparison.LookaheadsOnly:
                    throw new InvalidOperationException("LR(k) item sets cannot be tested for equal lookahead sets only.");
                case ProductionItemComparison.MarkedProductionAndLookaheads:
                default:
                    return _kernelItems.SetEquals(other.KernelItems);
            }
        }

        private HashSet<MarkedProduction<TNonterminalSymbol>> AsLr0KernelItems()
        {
            return new HashSet<MarkedProduction<TNonterminalSymbol>>(_kernelItems.Select(item => item.MarkedProduction));
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
            foreach (var item in KernelItems)
                hashCode = 31 * hashCode + item.GetHashCode();
            return hashCode;
        }

        public override string ToString()
        {
            return ClosureItems.Any()
                ? string.Concat(KernelItems.ToVectorString(), Environment.NewLine, ClosureItems.ToVectorString())
                : KernelItems.ToVectorString();
        }
    }
}
