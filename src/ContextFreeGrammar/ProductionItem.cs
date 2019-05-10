using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using AutomataLib;

namespace ContextFreeGrammar
{
    // TODO: Add IReadonlySet<TNonterminalSymbol> Lookaheads { get; } to item class => Both LR(0) and LR(1) items can be represented
    // TODO: Maybe better define Lr1Item := (Lr0Item, Lookaheads), because Lr(0) item can be reused betwenn LR(1) items
    // TODO: Kernel items (rename IsCore to IsKernel) are the only mandatory items. Closure items should be lazy.
    // NOTE: It is common for LR(1) item sets to have identical first components (i.e. identical LR(0) items),
    //       and only differ w.r.t different lookahead symbols (the second component). In construction LALR(1) we
    //       will look for different LR(1) items having the same (core) LR(0) items, and merge these into new union
    //       states (i.e. new one set of items). Since the GOTO (successor) function on;ly depends on the core LR(0) items
    //       of any LR91) items, it is easy to merge the transitions of the LR(1) automaton into a new simpler LALR automaton.
    //       On the other hand the ACTION table will change, and it is possible to introduce conflicts when merging.
    // TODO: Could be renamed to LRItem
    // TODO: Grammar should be read-only, make GrammarBuilder API
    // Different collections (set of items)
    //  - LR(0) collection (also used by SLR(1) parser)
    //  - LR(1) collection
    //  - LALR(1) collection (constructed in 2 different ways: Merging of LR(1) items, or, Efficient Construction)

    // TODO: 1) Build Lr1AutomataDFA, 2) Build paring table using modified SLR(1) algorithm where FOLLOW(A) is substituted by the Lookahead set
    // TODO: Again we have 2 ways to build LR(1) automaton: NFA -> DFA or directly
    /// <summary>
    /// The LR(0) item used as a building block in Donald Knuth's LR(0) Automaton. An LR(0) item is a dotted production rule,
    /// where everything to the left of the dot has been shifted onto the parsing stack and the next input token is in the
    /// FIRST set of the symbol following the dot (or in the FOLLOW set, if the next symbol is nullable). A dot at the right
    /// end indicates that we have shifted all RHS symbols onto the stack (i.e. we have recognized a handle), and that we can
    /// reduce that handle. A dot in the middle of the LR(0) item indicates that to continue further we need to shift a token
    /// that could start the symbol following the dot onto the stack. For example if the symbol following the dot is a nonterminal A
    /// then we want to shift something in FIRST(A) onto the stack. The action of adding equivalent LR(0) items to create a set
    /// of LR(0) items (a state of the DFA for the LR(0) automaton, aka configurating set of the LR parser) is called CLOSURE.
    /// </summary>
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
    public struct ProductionItem<TNonterminalSymbol, TTerminalSymbol> : IEquatable<ProductionItem<TNonterminalSymbol, TTerminalSymbol>>, IFiniteAutomatonState
        where TNonterminalSymbol : Symbol, IEquatable<TNonterminalSymbol>
        where TTerminalSymbol : Symbol, IEquatable<TTerminalSymbol>
    {
        private string DebuggerDisplay => ToString();
        private const char DOT = '•'; // Bullet

        private readonly int _dotPosition;

        public ProductionItem(
            Production<TNonterminalSymbol> production,
            int productionIndex,
            int dotPosition,
            params TTerminalSymbol[] lookaheads)
            : this(production, productionIndex, dotPosition, new InsertionOrderedSet<TTerminalSymbol>(lookaheads ?? Enumerable.Empty<TTerminalSymbol>()))
        {
        }

        public ProductionItem(
            Production<TNonterminalSymbol> production,
            int productionIndex,
            int dotPosition,
            IEnumerable<TTerminalSymbol> lookaheads = null)
            : this(production, productionIndex, dotPosition, new InsertionOrderedSet<TTerminalSymbol>(lookaheads ?? Enumerable.Empty<TTerminalSymbol>()))
        {
        }

        private ProductionItem(
            Production<TNonterminalSymbol> production,
            int productionIndex,
            int dotPosition,
            IReadOnlySet<TTerminalSymbol> lookaheads)
        {
            if (production == null)
            {
                throw new ArgumentNullException(nameof(production));
            }
            if (dotPosition > production.Tail.Count)
            {
                throw new ArgumentException("Invalid dot position --- The marker cannot be shifted beyond the end of the production.");
            }

            Production = production;
            ProductionIndex = productionIndex;
            _dotPosition = dotPosition;
            Lookaheads = lookaheads;
        }

        public Production<TNonterminalSymbol> Production { get; }

        public int ProductionIndex { get; }

        public IReadOnlySet<TTerminalSymbol> Lookaheads { get; }

        /// <summary>
        /// Any item B → α.β where α is not ε (the empty string),
        /// or the start rule S' → .S item (of the augmented grammar that
        /// is the first production of index zero by convention). That is
        /// the initial item S' → .S, and all items where the dot is not at the left end.
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

        // NOTE: we make a shallow copy of the read-only lookaheads set
        public ProductionItem<TNonterminalSymbol, TTerminalSymbol> GetNextItem() =>
            new ProductionItem<TNonterminalSymbol, TTerminalSymbol>(Production, ProductionIndex, _dotPosition + 1, Lookaheads);

        public bool Equals(ProductionItem<TNonterminalSymbol, TTerminalSymbol> other)
        {
            return Equals(other, ProductionItemComparison.Lr0ItemAndLookaheads);
        }

        public bool Equals(ProductionItem<TNonterminalSymbol, TTerminalSymbol> other, ProductionItemComparison comparison)
        {
            switch (comparison)
            {
                case ProductionItemComparison.Lr0ItemOnly:
                    return ProductionIndex == other.ProductionIndex && _dotPosition == other._dotPosition;
                case ProductionItemComparison.LookaheadsOnly:
                    return Lookaheads.SetEquals(other.Lookaheads);
                case ProductionItemComparison.Lr0ItemAndLookaheads:
                default:
                    return ProductionIndex == other.ProductionIndex && _dotPosition == other._dotPosition && Lookaheads.SetEquals(other.Lookaheads);
            }
        }

        public override bool Equals(object obj)
        {
            return obj is ProductionItem<TNonterminalSymbol, TTerminalSymbol> item && Equals(item);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = _dotPosition;
                hashCode = (hashCode * 397) ^ ProductionIndex;
                hashCode = (hashCode * 397) ^ Lookaheads.GetHashCode();
                return hashCode;
            }
        }

        string IFiniteAutomatonState.Id => $"{ProductionIndex}_{_dotPosition}";

        string IFiniteAutomatonState.Label => ToString();

        public override string ToString()
        {
            ProductionItem<TNonterminalSymbol, TTerminalSymbol> self = this;

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

            return Lookaheads.Count == 0
                ? $"{Production.Head} → {dottedTail}"
                : $"[{Production.Head} → {dottedTail}, {string.Join("/", Lookaheads)}]";
        }
    }

    public enum ProductionItemComparison
    {
        Lr0ItemAndLookaheads = 0,
        Lr0ItemOnly,
        LookaheadsOnly
    }
}
