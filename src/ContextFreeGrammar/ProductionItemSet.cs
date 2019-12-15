using System;
using System.Collections;
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
    public class ProductionItemSet<TTokenKind> : IEquatable<ProductionItemSet<TTokenKind>>, IReadOnlySet<ProductionItem<TTokenKind>>
        where TTokenKind : struct, Enum
    {
        private string DebuggerDisplay => ClosureItems.Any()
            ? string.Concat(KernelItems.ToVectorString(), ":", ClosureItems.ToVectorString())
            : KernelItems.ToVectorString();

        private readonly bool _anyLookaheads; // are comparisons of items and marked productions the same thing?

        // kernel items are always non-empty (the kernel items identifies the LR(0) item set)
        private readonly HashSet<ProductionItem<TTokenKind>> _kernelItems;
        // closure items can be empty (and can BTW always be generated on the fly, but we store them to begin with)
        private readonly List<ProductionItem<TTokenKind>> _closureItems;

        public ProductionItemSet(IEnumerable<ProductionItem<TTokenKind>> items)
        {
            _kernelItems = new HashSet<ProductionItem<TTokenKind>>();
            _closureItems = new List<ProductionItem<TTokenKind>>();
            foreach (var item in items)
            {
                if (item.IsKernelItem)
                    _kernelItems.Add(item);
                else
                    _closureItems.Add(item);
                _anyLookaheads = _anyLookaheads || item.Lookaheads.Count > 0;
            }
        }

        public ProductionItem<TTokenKind> ReduceBy(int productionIndex) =>
            _kernelItems.Single(item => item.IsReduceItem && item.ProductionIndex == productionIndex);

        /// <summary>
        /// Because all transitions entering any given state in the DFA for the LR(0) automaton have the same label,
        /// the LR(0) item set has a unique spelling property, that can be used to compute the sentential form
        /// during shift/reduce parsing.
        /// </summary>
        public Symbol SpellingSymbol => KernelItems.First().BeforeDotSpellingSymbol; // all kernel items have the same grammar symbol to the left of the dot

        public IEnumerable<ProductionItem<TTokenKind>> Items => _kernelItems.Concat(_closureItems);

        /// <summary>
        /// The partially parsed rules for a state are called its kernel LR(0) items.
        /// If we also call S′ → .S a kernel item, we observe that every state in the
        /// DFA is completely determined by its subset of kernel items.
        /// </summary>
        public IEnumerable<ProductionItem<TTokenKind>> KernelItems => _kernelItems;

        /// <summary>
        /// The closure items (obtained via ϵ-closure) do not determine the state of the LR(0) automaton,
        /// because they can all be forgotten about, and regenerated on the fly. All closure items have
        /// the dot at the beginning of the rule, and are therefore not parsed yet.
        /// </summary>
        public IEnumerable<ProductionItem<TTokenKind>> ClosureItems => _closureItems;

        private ProductionItem<TTokenKind>[] _reduceItems;

        /// <summary>
        /// The ordered list of reduce items, where a reduce item comes first, if it is based on a
        /// production that comes first in the grammar specification.
        /// </summary>
        /// <remarks>
        /// If grammar has no ε-productions, then all (completed) reduce items are kernel items,
        /// because the single item of an ε-production is both a reduce item and and a closure item
        /// (it can never be a kernel item).
        /// </remarks>
        public IReadOnlyList<ProductionItem<TTokenKind>> ReduceItems => _reduceItems ??= GetReduceItems();

        ProductionItem<TTokenKind>[] GetReduceItems()
        {
            // In case of reduce-reduce conflicts we order by production index (standard conflict resolution)
            var reduceItems = Items.Where(item => item.IsReduceItem).ToArray();
            return reduceItems.Length <= 1
                ? reduceItems // this is the typical case
                : reduceItems.OrderBy(item => item.ProductionIndex).ToArray();
        }

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
        public IEnumerable<Nonterminal> NonterminalTransitions =>
            Items.Where(item => item.DotSymbol.IsNonterminal).Select(item => item.GetDotSymbol<Nonterminal>());

        /// <summary>
        /// Find all terminal labeled transitions out of this item set.
        /// </summary>
        public IEnumerable<Terminal<TTokenKind>> TerminalTransitions =>
            Items.Where(item => item.DotSymbol.IsTerminal).Select(item => item.GetDotSymbol<Terminal<TTokenKind>>());

        /// <summary>
        /// Compute the successor goto items (i.e for non-terminal transitions) and/or shift items
        /// (i.e. for terminal transitions). This is the kernel items of the GOTO function in the dragon book.
        /// </summary>
        public ILookup<Symbol, ProductionItem<TTokenKind>> GetTargetItems()
        {
            return Items
                .Where(item => !item.IsReduceItem)
                .ToLookup(item => item.DotSymbol, item => item.WithShiftedDot());
        }

        public bool Equals(ProductionItemSet<TTokenKind> other)
        {
            return other != null && _kernelItems.SetEquals(other.KernelItems);
        }

        /// <summary>
        /// Does this LR(k) item set have kernel items which CORE equals the given marked productions?
        /// </summary>
        public bool CoreOfKernelEquals(IEnumerable<MarkedProduction> other)
        {
            return CoreOfKernelEquals(other.Select(dottedProduction => dottedProduction.AsLr0Item<TTokenKind>()));
        }


        /// <summary>
        /// Does this LR(k) item set have kernel items which CORE equals the given marked productions?
        /// </summary>
        public bool CoreOfKernelEquals(params MarkedProduction[] other)
        {
            return CoreOfKernelEquals(other.Select(dottedProduction => dottedProduction.AsLr0Item<TTokenKind>()));
        }

        /// <summary>
        /// Are the CORE of the kernel items of the two item sets the same?
        /// </summary>
        public bool CoreOfKernelEquals(IEnumerable<ProductionItem<TTokenKind>> otherItemSet)
        {
            // we filter out closure items in the otherItemSet, because this way we can compare ProductionItemSet instances
            return _anyLookaheads
                ? AsLr0KernelItems().SetEquals(otherItemSet.Where(item => item.IsKernelItem).Select(item => item.MarkedProduction))
                : _kernelItems.SetEquals(otherItemSet.Where(item => item.IsKernelItem));

            HashSet<MarkedProduction> AsLr0KernelItems()
            {
                return new HashSet<MarkedProduction>(_kernelItems.Select(item => item.MarkedProduction));
            }
        }

        /// <summary>
        /// Does this LR(k) item set contain a kernel item with a given CORE (dotted production).
        /// </summary>
        public bool CoreOfKernelContains(MarkedProduction dottedProduction)
        {
            return _anyLookaheads
                ? KernelItems.Select(item => item.MarkedProduction).Contains(dottedProduction)
                : _kernelItems.Contains(dottedProduction.AsLr0Item<TTokenKind>());
        }

        public override bool Equals(object obj)
        {
            return obj is ProductionItemSet<TTokenKind> set && Equals(set);
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

        public IEnumerator<ProductionItem<TTokenKind>> GetEnumerator()
        {
            return Items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count => _kernelItems.Count + _closureItems.Count;

        public bool Contains(ProductionItem<TTokenKind> item)
        {
            return _kernelItems.Contains(item) || _closureItems.Contains(item);
        }

        /// <inheritdoc />
        public bool IsSubsetOf(IEnumerable<ProductionItem<TTokenKind>> other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));
            var otherHashset = new HashSet<ProductionItem<TTokenKind>>(other);
            return otherHashset.IsSupersetOf(this);
        }

        /// <inheritdoc />
        public bool IsSupersetOf(IEnumerable<ProductionItem<TTokenKind>> other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));
            return other.All(Contains);
        }

        /// <inheritdoc />
        public bool IsProperSupersetOf(IEnumerable<ProductionItem<TTokenKind>> other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));
            var otherHashset = new HashSet<ProductionItem<TTokenKind>>(other);
            return otherHashset.IsProperSubsetOf(this);
        }

        /// <inheritdoc />
        public bool IsProperSubsetOf(IEnumerable<ProductionItem<TTokenKind>> other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));
            var otherHashset = new HashSet<ProductionItem<TTokenKind>>(other);
            return otherHashset.IsProperSupersetOf(this);
        }

        public bool Overlaps(IEnumerable<ProductionItem<TTokenKind>> other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));
            return other.Any(Contains);
        }

        public bool SetEquals(IEnumerable<ProductionItem<TTokenKind>> other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));
            var otherHashset = new HashSet<ProductionItem<TTokenKind>>(other);
            return otherHashset.SetEquals(this);
        }
    }
}
