using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

    //--------------------------------------------------------------------------------------------------
    // Def: Viable prefixes are those prefixes of right sentential forms that can appear on the stack of
    //      a shift-reduce parser.
    //--------------------------------------------------------------------------------------------------
    // Goal: Recognize substrings of grammar symbols that can appear on the stack. The stack contents
    //       must be a prefix (called a viable prefix) of some right sentential form. If stack holds 𝛿αβ,
    //       and the remaining input is v, then 𝛿αβ can be reduced to S' in one or more reductions.
    //
    // LR(0) item A → α•β is valid for a viable prefix 𝛿α, if there is a rightmost derivation
    //
    //      S' *⇒ 𝛿Av ⇒ 𝛿αβv,    where v in Pow(T) (v has only terminal symbols)
    //
    // LR(1) item [A → α•β, b] is valid for a viable prefix 𝛿α, if there is a rightmost derivation
    //
    //      S' *⇒ 𝛿Av ⇒ 𝛿αβv,    where v in Pow(T) (v has only terminal symbols)
    // and
    //      v = bw    or    (v = ε and b = $)       (LR(1)-lookahead definition)
    //
    // LR(1)-lookahead definition: b is the first symbol in v, or b is $ (eof), if v is the empty string
    //
    // NOTE: Parser will not shift passed the handle (αβ), and therefore we will recognize rightmost handles.
    //
    // The set of LR(0)-characteristic strings (completed items == accept states of DFA)
    //
    //    CG0 = {𝛿β ∈ Pow(V) | S′ ∗⇒ 𝛿Av ⇒ 𝛿βv, 𝛿β ∈ Pow(V), v ∈ Pow(T)}, where V := N U V (all grammar symbols)
    //
    // The set of LR(1)-characteristic strings (completed items == accept states of DFA)
    //
    //    CG1 = {𝛿β ∈ Pow(V) | S′ ∗⇒ 𝛿Av ⇒ 𝛿βv, 𝛿β ∈ Pow(V), v ∈ Pow(T)}, where V := N U V (all grammar symbols)
    //          where each item is carrying lookahead symbol for follow-condition on reductions
    //--------------------------------------------------------------------------------------------------

    /// <summary>
    /// The LR(k) item used as a building block in Donald Knuth's LR(k) Automaton, and in all LR shift-reduce
    /// parsers (LR(0), SLR(1), LALR(1) and LR(1)).
    ///
    /// An LR(0) item [B → α•β, {}] is a dotted production rule, where everything to the left of the dot has been shifted onto
    /// the stack and the next input token is in the set FIRST(β) (or in the FOLLOW(B) set, if β is nullable).
    ///
    /// A dot at the right end indicates that we have shifted all RHS symbols onto the stack (i.e. we have recognized a handle),
    /// and that we can reduce that handle. A dot in the middle of the item indicates that to continue further we need to shift a token
    /// that could start the symbol following the dot onto the stack. For example if the symbol following the dot is a nonterminal A
    /// then we want to shift something in FIRST(A) onto the stack.
    ///
    /// An LR(1) item [B → α•β, {b}] is a dotted production rule that have been augmented with information about what subset
    /// of the follow set is appropriate given the path we have taken to that state. Again an item B → α•β indicates that
    /// symbols α have been pushed on to the stack (i.e. states that spell out α is on the stack), and we are expecting to put
    /// states corresponding to the symbols β on the stack and then reduce, but only if the token following β is the terminal b.
    /// The symbol b is called the lookahead of the item. LR(1) items are born with a single lookahead symbol in every item, but
    /// after computing CLOSURE (The action of adding equivalent LR(k) items to create a set of LR(k) items is called CLOSURE) of
    /// every item set (subset construction equivalent), we will often merge lookahead symbols into its union set, of any items with
    /// similar LR(0) item part. Thus the 'merged' item [B → α•, {a,b,c}] says that it is okay to reduce α to B if the next token
    /// is equal to one of {a,b,c}.
    /// </summary>
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
    public struct ProductionItem<TNonterminalSymbol, TTerminalSymbol> : IEquatable<ProductionItem<TNonterminalSymbol, TTerminalSymbol>>, IFiniteAutomatonState
        where TNonterminalSymbol : Symbol, IEquatable<TNonterminalSymbol>
        where TTerminalSymbol : Symbol, IEquatable<TTerminalSymbol>
    {
        private string DebuggerDisplay => ToString();
        private static readonly IReadOnlySet<TTerminalSymbol> s_emptyLookaheads = new Set<TTerminalSymbol>(Enumerable.Empty<TTerminalSymbol>());

        public ProductionItem(
            Production<TNonterminalSymbol> production,
            int productionIndex,
            int markerPosition,
            params TTerminalSymbol[] lookaheads)
            : this(new MarkedProduction<TNonterminalSymbol>(production, productionIndex, markerPosition),
                   new Set<TTerminalSymbol>(lookaheads ?? Enumerable.Empty<TTerminalSymbol>()))
        {
        }

        public ProductionItem(
            Production<TNonterminalSymbol> production,
            int productionIndex,
            int markerPosition,
            IEnumerable<TTerminalSymbol> lookaheads = null)
            : this(new MarkedProduction<TNonterminalSymbol>(production, productionIndex, markerPosition),
                   new Set<TTerminalSymbol>(lookaheads ?? Enumerable.Empty<TTerminalSymbol>()))
        {
        }

        public ProductionItem(
            MarkedProduction<TNonterminalSymbol> markedProduction,
            IReadOnlySet<TTerminalSymbol> lookaheads)
        {
            MarkedProduction = markedProduction;
            Lookaheads = lookaheads ?? s_emptyLookaheads;
        }

        /// <summary>
        /// Merge lookaheads into new LR(1) item.
        /// </summary>
        /// <returns></returns>
        public ProductionItem<TNonterminalSymbol, TTerminalSymbol> WithUnionLookaheads(IEnumerable<TTerminalSymbol> lookaheadsToAdd) =>
            new ProductionItem<TNonterminalSymbol, TTerminalSymbol>(MarkedProduction, new Set<TTerminalSymbol>(Lookaheads).Union(lookaheadsToAdd));

        /// <summary>
        /// Convert to new LR(0) item with empty lookahead set.
        /// </summary>
        /// <returns></returns>
        public ProductionItem<TNonterminalSymbol, TTerminalSymbol> WithNoLookahead() =>
            new ProductionItem<TNonterminalSymbol, TTerminalSymbol>(MarkedProduction, s_emptyLookaheads);

        /// <summary>
        /// Get the successor item of a shift/goto action created by 'shifting the dot'.
        /// </summary>
        public ProductionItem<TNonterminalSymbol, TTerminalSymbol> WithShiftedDot()
            // NOTE: we only make a shallow copy of the read-only lookaheads set
            => new ProductionItem<TNonterminalSymbol, TTerminalSymbol>(MarkedProduction.WithShiftedDot(), Lookaheads);

        public MarkedProduction<TNonterminalSymbol> MarkedProduction { get; }

        public Production<TNonterminalSymbol> Production => MarkedProduction.Production;

        public int ProductionIndex => MarkedProduction.ProductionIndex;

        public int MarkerPosition => MarkedProduction.MarkerPosition;

        /// <summary>
        /// The value(s) of the lookahead (b) that can follow the recognized handle (α) of a recognized
        /// completed item [A → α•, b] on the stack. The parser will only perform the the reduction and pop
        /// |α| symbols off the stack, and push (GOTO(s, A)) if the lookahead is equal to 'b. The lookahead
        /// part of the LR item is only used for LR(1) items. LR(0) items do not carry any lookahead, and
        /// therefore the set is empty for LR(0) items.
        /// </summary>
        public IReadOnlySet<TTerminalSymbol> Lookaheads { get; }

        /// <summary>
        /// Any item B → α•β where α is not ε (the empty string),
        /// or the start rule S' → •S item (of the augmented grammar that
        /// is the first production of index zero by convention). That is
        /// the initial item S' → •S, and all items where the dot is not at the left end.
        /// </summary>
        public bool IsCoreItem => MarkedProduction.IsCoreItem;

        /// <summary>
        /// Is this item a completed item on the form A → α•, where the dot have been shifted
        /// all the way to the end of the production (a completed item is an accepting state,
        /// where we have recognized a handle)
        /// </summary>
        public bool IsReduceItem => MarkedProduction.IsReduceItem;

        /// <summary>
        /// B → α•Xβ (where X is a nonterminal symbol)
        /// </summary>
        public bool IsGotoItem => MarkedProduction.IsGotoItem;

        /// <summary>
        /// B → α•aβ (where a is a terminal symbol)
        /// </summary>
        public bool IsShiftItem => MarkedProduction.IsShiftItem;

        /// <summary>
        /// Get the symbol before the dot.
        /// </summary>
        public Symbol GetPrevSymbol() => MarkedProduction.GetPrevSymbol();

        /// <summary>
        /// Get the symbol after the dot.
        /// </summary>
        public Symbol GetNextSymbol() => MarkedProduction.GetNextSymbol();

        /// <summary>
        /// Get the symbol after the dot.
        /// </summary>
        public TSymbol GetNextSymbol<TSymbol>() where TSymbol : Symbol => MarkedProduction.GetNextSymbol<TSymbol>();

        /// <summary>
        /// Get the symbol after the dot.
        /// </summary>
        public TSymbol GetNextSymbolAs<TSymbol>() where TSymbol : Symbol => MarkedProduction.GetNextSymbolAs<TSymbol>();

        /// <summary>
        /// Get the remaining symbols after the dot.
        /// </summary>
        public IEnumerable<Symbol> GetRemainingSymbolsAfterNextSymbol() => MarkedProduction.GetRemainingSymbolsAfterNextSymbol();

        public bool Equals(ProductionItem<TNonterminalSymbol, TTerminalSymbol> other)
        {
            return Equals(other, ProductionItemComparison.MarkedProductionAndLookaheads);
        }

        public bool Equals(ProductionItem<TNonterminalSymbol, TTerminalSymbol> other, ProductionItemComparison comparison)
        {
            switch (comparison)
            {
                case ProductionItemComparison.MarkedProductionOnly:
                    return MarkedProduction.Equals(other.MarkedProduction);
                case ProductionItemComparison.LookaheadsOnly:
                    return Lookaheads.SetEquals(other.Lookaheads);
                case ProductionItemComparison.MarkedProductionAndLookaheads:
                default:
                    return MarkedProduction.Equals(other.MarkedProduction) && Lookaheads.SetEquals(other.Lookaheads);
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
                var hashCode = MarkedProduction.GetHashCode();
                hashCode = (hashCode * 397) ^ Lookaheads.GetHashCode();
                return hashCode;
            }
        }

        // $ is an illegal character for en identifier in a Graphviz DOT file
        string IFiniteAutomatonState.Id => Lookaheads.Count == 0
            ? $"{ProductionIndex}_{MarkerPosition}"
            : $"{ProductionIndex}_{MarkerPosition}_{string.Join("_", Lookaheads.Select(s => s.IsEof ? "eof" : s.Name))}";

        string IFiniteAutomatonState.Label => ToString();

        public override string ToString()
        {
            return Lookaheads.Count == 0
                ? $"{MarkedProduction}"
                : $"[{MarkedProduction}, {string.Join("/", Lookaheads)}]";
        }
    }

    public enum ProductionItemComparison
    {
        MarkedProductionAndLookaheads = 0,
        MarkedProductionOnly,
        LookaheadsOnly
    }
}
