using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AutomataLib;

namespace ContextFreeGrammar
{
    // TODO: Kernel items (rename Iskernel to IsKernel) are the only mandatory items. Closure items should be lazy.
    // TODO: Could be renamed to LRItem

    #region docs

    // Different collections (set of items)
    //  - LR(0) collection (also used by SLR(1) parser), aka LR(0) or SLR(1) (when using simple Follow sets)
    //  - LR(1) collection (The Canonical LR(1) parser, aka CLR(1))
    //  - LALR(1) collection (constructed in 2 different ways: Merging of LR(1) items, or, Efficient Construction)

    // NOTE: valid prefix = viable prefix = characteristic string

    // Valid Items and Prefixes
    // ========================
    // Definition: A marked production A → α•β is a _valid_ LR(0) item for a string δα in V∗ if
    //      S′ ∗⇒ δAv ⇒ δαβv = γβv
    // If such a derivation holds in G, then γ = δα in V∗ is a valid prefix.
    //
    // Definition: We say that 'γ = δα _accesses_ the state' q containing A → α•β. This is often written
    //        * γ accesses q
    //        * s0 --γ--> q
    //        * q = [γ]
    //        * In state q we have pushed states that spell out γ onto the stack, therefore we have pushed γ onto that stack
    //
    // The transitions of the DFA van can be analyzed via splitting the valid prefixes into transitions
    // A pair ([δ],X) in Q × V is a transition if and only if δX is a valid prefix. If this is the
    // case, then [δX] is the state accessed upon reading δX, thus the notation [δX] also implies (we
    // always assume when writing [δX] that Valid(δX) is not the empty set) a transition from [δ] on X,
    // into [δX]. This is written [δ]--X-->[δX], and we say that [δX] is accessed from [δ] on the path X,
    // where |X|=1). The notation can be extended to non-singleton paths, where [δα] is accessed from [δ]
    // on a path equal to α.
    //
    // The set of valid items for a given string γ in V∗ is denoted by Valid(γ). Therefore Valid(γ) denotes a state of
    // the LR(0) automaton also called an LR(0) item set.
    // Two strings δ and γ are equivalent if and only if they have the same valid items.
    // The valid item sets are obtained through the following computations:
    //
    // The function Kernel(γX) yields the kernel items (aka kernel items) reached by a transition on symbol X from the
    // state Valid(γ):
    //
    //      Kernel(ε) = {S′ → •S$}                                      (base case for kernel items)
    //      Kernel(γX) = {A → αX•β | A → α•Xβ ∈ Valid(γ)}              (induction/transition step for kernel items)
    //
    //      Valid(γ) = Kernel(γ) ∪ Closure(Valid(γ)).                  (closure of item set: from kernel items to closure items)
    //
    // where the function Closure(I) yields the result of adding closure items to the kernel items of a LR(0) item set I.
    //
    //      Closure(I) = {B → •ω | A → α•Bβ ∈ I}
    //
    // NOTE: Since states are uniquely determined by their sets of kernel items, the test to see if a new state have
    // been found does not require that the Closure function be applied to the kernel items first.
    //
    // LR(0) Automaton
    // ===============
    // LR(0) automaton is a deterministic pushdown automaton (DPDA) that uses equivalence classes on valid
    // prefixes as their stack alphabet Q. We therefore denote explicitly states of a LR parser as q = [δ],
    // where δ is some valid prefix in the state q reached upon reading this prefix (s0 --δ--> q is therefore
    // denoted q = [δ]). For instance, in the automaton of Figure 1a, state q2 is the equivalence class {S},
    // while state q8 is the equivalence class described by the regular language Aa∗a.
    //
    // LALR(1) Automaton
    // =================
    // The LALR(1) lookahead set of a reduction using A → α in state q is
    //      LA(q, A → α) = {a ∈ T | S′ *⇒ δAav ⇒ δαav and q = [δα], v ∈ N*}
    // That is given the DFA has reached a state [δα] containing the reduce item A → α•, where we consider to
    // reduce and backtrack to the predecessor state p (after popping |α| states/symbols of the stack),
    // because we know [δ]--α-->[δα], we collect all terminal symbols that can follow the GOTO action (non-terminal)
    // transition on A, i.e. where [δAa] can be accessed such that δAav is a valid right-most sentential form.
    //
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
    //
    // For any reduce state (LR(0) item set containing a reduce item), and any reducing production A → β in q, let
    //
    //    LA(q, A → β) := {b ∈ T | S′ ∗⇒ 𝛿Abv ⇒ 𝛿βbv, 𝛿β ∈ Pow(V), v ∈ Pow(T), 𝛿β accesses q},
    //    LA(q, A → β) := {1:v | S′ *⇒ δAv and q = [δβ]}
    //
    // where V := N U V (all grammar symbols). In words LA(q, A → β) consists of the terminal symbols for
    // which the reduction by production A → β in state q is the correct action. That 𝛿β accesses q means it
    // is a viable prefix found/recognized in state q.
    //
    // For any state p and any nonterminal A, let
    //
    //    FOLLOW(p, A) := {b ∈ T | S′ ∗⇒ 𝛿Abv, 𝛿 ∈ Pow(V), v ∈ Pow(T), 𝛿 accesses p}
    //
    // Propagation of lookahead symbols into state p after having reduced 𝛿β into 𝛿A by production A → β
    // on the stack (just before pushing GOTO(p, A) onto the stack).
    //
    // Since for any derivation
    //
    //         S′ ∗⇒ 𝛿Abv ⇒ 𝛿βbv
    //
    // where 𝛿β accesses q, there is a state p such that p --β--> q and 𝛿 accesses p, it is
    //--------------------------------------------------------------------------------------------------

    // TODO: Change Func to lookaheadSetResolver in ComputeParsingTable
    // Definition: Lookahead set
    // For a non-terminal A we define a lookahead set to be any set of terminals which
    // immediately follow an instance of A in the grammar. A reduction (A → α•) is only
    // applicable if the next input symbol, the lookahead, appears in the given lookahead
    // set of A.


    // Dragon Book Algorithm on finding LA-sets
    //
    // If the existence of some item, I1, in some state implies the existence of another item, I2, either
    // in the same state (through the addition of closure items) or in some other state (through
    // the state completion process), then the lookahead function applied to I2 yields a set which may
    // contain symbols determined by I1. This is called spontaneous generation of lookahead symbols.
    // In addition, it is possible that the set of lookahead symbols for I2 must include the entire set of
    // lookahead symbols for I1. In this case, the symbols are said to propagate from I1 to I2.

    // spontaneous generation
    // ======================
    // If the existence of some item, I1, in some state implies the existence of another item, I2, either
    // in the same state (through the addition of completion/closure items) or in some other state (through the
    // state completion process), then the lookahead function applied to I2 yields a set which may contain
    // symbols determined by I1. This is called spontaneous generation of lookahead symbols.
    //
    // propagate
    // =========
    // In addition, it is possible that the set of lookahead symbols for I2 must include the entire set of
    // lookahead symbols for I1. In this case, the symbols are said to propagate from I1 to I2.
    //
    // The rules for spontaneous generation of symbols and propagation of symbols in the two possible
    // settings are as follows.
    //
    // Case 1 -- Closure Items
    // Suppose that state q contains an item I1, where the marker appears to the left of a nonterminal symbol.
    // That is, I1 has the form [A → α•Xβ]. The state must also contain one or more closure items with the form
    // [B → •ω]. Let I2 be one such item.
    // The symbols which can follow the RHS of I2 must include the symbols which follow X in item I1, and the
    // symbols which can follow X in that item must include FIRST(β). In other words, a ∈ FIRST(β) implies
    // a ∈ LA(q, I2). In the terminology of the dragon book, the symbol a is spontaneously generated (by I1)
    // and must appear in the lookahead set of I2.
    // In addition, if β is nullable and b ∈ LA(q, I1), then b ∈ LA(q, I2) must hold. This would be a case
    // of symbol b propagating from I1 to I2.
    //
    // Case 2 -- Kernel Items
    // Suppose that state q1 contains an item I1 with the form [A → α•Xβ]. There must necessarily be another
    // state q2 reached by a transition on symbol X from q1, where q2 contains a kernel item with the form
    // [A → αX•β]. In such a case, if a ∈ LA(q1, I1) then a ∈ LA(q2, I2). This is another example of propagation,
    // where symbol a propagates from I1 to I2.
    //
    // A simple algorithm to determine the lookahead sets can start by initializing all lookahead sets
    // to empty. Then it can make repeated passes over all items in all states adding spontaneously
    // generated symbols and propagated symbols to the sets. This iterative procedure can halt when
    // a pass fails to add any new symbols to any set. A faster version, would use a worklist so that
    // only items whose lookahead sets have changed participate in the next pass. Entries in the worklist
    // consist of (state, item)-pairs.
    #endregion

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
            Lookaheads = lookaheads ?? Set<TTerminalSymbol>.Empty;
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
            new ProductionItem<TNonterminalSymbol, TTerminalSymbol>(MarkedProduction, Set<TTerminalSymbol>.Empty);

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
        /// completed item [A → α•, b] on the stack. The parser will only perform the reduction and pop
        /// |α| symbols off the stack, and push (GOTO(s, A)) if the lookahead is equal to b. The lookahead
        /// part of the LR item is only used for LR(1) items. LR(0) items do not carry any lookahead, and
        /// therefore the set is empty for LR(0) items.
        /// </summary>
        public IReadOnlySet<TTerminalSymbol> Lookaheads { get; }

        /// <summary>
        /// Any item B → α•β where α is not ε (the empty string),
        /// or the start S' → •S item (of the augmented grammar). That is
        /// the initial item S' → •S, and all other items where the dot is not
        /// at the left end are considered kernel items.
        /// </summary>
        public bool IsKernelItem => MarkedProduction.IsKernelItem;

        /// <summary>
        /// Any item A → •β where the dot is at the beginning of the RHS of the production,
        /// except the initial item S' → •S.
        /// </summary>
        public bool IsClosureItem => MarkedProduction.IsClosureItem;

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
        /// All kernel items (of any item set) have the same symbol before the dot.
        /// If the item is a closure item the result is the empty symbol (ε).
        /// </summary>
        public Symbol SpellingSymbol => MarkedProduction.SpellingSymbol;

        /// <summary>
        /// The symbol after the dot. If the dot have been shifted all the way to the end of the RHS of
        /// the production the result is the empty symbol (ε).
        /// </summary>
        public Symbol DotSymbol => MarkedProduction.DotSymbol;

        /// <summary>
        /// Get the symbol after the dot.
        /// </summary>
        public TSymbol GetDotSymbol<TSymbol>() where TSymbol : Symbol => MarkedProduction.GetDotSymbol<TSymbol>();

        /// <summary>
        /// Get the symbol after the dot.
        /// </summary>
        public TSymbol TryGetDotSymbol<TSymbol>() where TSymbol : Symbol => MarkedProduction.TryGetDotSymbol<TSymbol>();

        /// <summary>
        /// Get the remaining symbols before the dot symbol in reverse order.
        /// </summary>
        public IEnumerable<Symbol> GetRemainingSymbolsBeforeDotSymbol() => MarkedProduction.GetRemainingSymbolsBeforeDotSymbol();

        /// <summary>
        /// Get the remaining symbols after the dot symbol in normal order.
        /// </summary>
        public IEnumerable<Symbol> GetRemainingSymbolsAfterDotSymbol() => MarkedProduction.GetRemainingSymbolsAfterDotSymbol();

        public bool Equals(ProductionItem<TNonterminalSymbol, TTerminalSymbol> other)
        {
            return Equals(other, ProductionItemComparison.MarkedProductionAndLookaheads);
        }

        public bool Equals(ProductionItem<TNonterminalSymbol, TTerminalSymbol> other, ProductionItemComparison comparison)
        {
            switch (comparison)
            {
                case ProductionItemComparison.MarkedProductionOnly:
                    return MarkedProduction.Equals(other.MarkedProduction); // CORE is the same
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
