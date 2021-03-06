using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using AutomataLib;
using AutomataLib.Graphs;

namespace ContextFreeGrammar.Analyzers
{
    // Motivation for looking at nonterminal transitions in the LR(0) automaton
    // ========================================================================
    // Let the pair (p,A) represent a nonterminal transition from state p labeled with nonterminal symbol
    // in the graph of the LR(0) automaton (CGA). That is GOTO(p,A) is defined somewhere in the graph. Because
    // the CGA is deterministic the pair is unique. Also the state p of the pair (p,A) must contain an item on
    // the form [B → β•Aγ]. It can either be a kernel item or a closure item.
    //
    // kernel item with nonterminal dot symbol:     B → β•Aγ
    // closure item with nonterminal dot symbol:    B → •Aγ
    //
    // where dot symbol of an item is the symbol after the dot. When a shift-reduce parser decide to reduce the
    // symbols (states) on top of the stack by a production A → ω, by popping |ω| symbols (states) of the stack,
    // and pushing the A symbol to finalize the reduction, we need to ensure that the lookahead symbol (next terminal
    // input symbol) is valid given CGA machine/automaton. We therefore need to simulate/analyze what terminal symbols
    // can follow the possible terminal (p,A)-transitions. This is done in 3 steps:
    //
    // Graph Read
    //
    //
    // Graph LA
    //
    //
    // Union of predecessor (lookback) (p,A)-transitions

    // direct reads
    //      DR(p,A) = {a ∈ T | GOTO(p,Aa) is defined }
    // indirect reads
    //      (p,A) reads (r,C)
    //           iff
    //      GOTO(p,A) = r, GOTO(r,C) = s for some state s in the LR(0) automaton, and C *=> ε (C is nullable/erasable)

    public static class LalrLookaheadSetsAlgorithm
    {
        /// <summary>
        /// Vertices are defined by all nonterminal ('goto') transitions in the LR(0) automaton denoted by a pairs on the form (p,A).
        /// </summary>
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public static IReadOnlyOrderedSet<(int, Nonterminal)> GetGotoTransitionPairs<TTokenKind, TNonterminal>(
            Grammar<TTokenKind, TNonterminal> grammar,
            LrItemsDfa<TTokenKind> dfaLr0
            )
            where TTokenKind : struct, Enum
            where TNonterminal : struct, Enum
        {
            // This is the bijective mapping between integer values and (p,A)-pairs.
            var vertices = new InsertionOrderedSet<(int, Nonterminal)>();

            // p = 1,...,N
            foreach (int p in dfaLr0.GetTrimmedStates())
                // any items of the form [B → β•Aγ] contained by the underlying item set of the state p
                foreach (var A in dfaLr0.GetUnderlyingState(p).NonterminalTransitions)
                    vertices.Add((p, A));

            return vertices;
        }

        /// <summary>
        /// Vertices are defined by all nonterminal ('goto') transitions in the LR(0) automaton denoted by a pairs on the form (p,A).
        /// We are trying to solve for the set-valued Read(p,A) function defined by
        ///      Read(p,A) = {a ∈ T | S *=> αAβav => αAav, α ∈ V∗, β ∈ V+, β +=> ε, GOTO(q0,α) = p}, i.e. β=X1..Xn, where E(Xi)=true, for all i
        ///                = DR(p,A) ∪ { Read(r,C) | (p,A) reads+ (p,C) }
        ///                = ∪ { DR(r,C) : (p,A) reads* (p,C) }
        ///
        ///      Read(p,A) = DR(p,A) U ∪{ Read(r,C) | (p,A) reads (r,C) }
        ///
        /// That is all singleton terminal symbols that can be read before any phrase including A is reduced.
        ///
        /// direct reads (init sets)
        ///      DR(p,A) = {a ∈ T | GOTO(p,Aa) is defined }
        /// indirect reads (relation, edges in digraph)
        ///      (p,A) reads (r,C)
        ///           iff
        ///      GOTO(p,A) = r, GOTO(r,C) = s for some state s in the LR(0) automaton, and C *=> ε (C is nullable/erasable)
        ///           iff
        ///      GOTO(p,AC) is defined, and C *=> ε
        /// </summary>
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public static (ImmutableArray<IReadOnlySet<Terminal<TTokenKind>>> DR, IGraph Graph) GetGraphReads<TTokenKind, TNonterminal>(
            Grammar<TTokenKind, TNonterminal> grammar,
            LrItemsDfa<TTokenKind> dfaLr0,
            IReadOnlyOrderedSet<(int, Nonterminal)> vertices,
            IErasableSymbolsAnalyzer analyzer
            )
            where TTokenKind : struct, Enum
            where TNonterminal : struct, Enum
        {
            // TODO: Move everywhere (maybe an invariant of grammar itself...If (!IsAugmented) MakeAugmented
            if (!grammar.IsAugmented)
                throw new ArgumentException(
                    "The first production of the grammar must be a unit production on the form S' → S, where S' is found no where else in any productions ");

            var edges = new HashSet<(int, int)>(); // no parallel edges

            // direct reads (init sets)
            var DR = new Set<Terminal<TTokenKind>>[vertices.Count]; // terminals, including eof = $
            for (int i = 0; i < vertices.Count; i += 1)
                DR[i] = new Set<Terminal<TTokenKind>>();

            // Ensure that for the state p whose kernel item is [S' → •S], we have
            //       DR(p, S) = {$},
            // _even_ if the grammar haven't been augmented with an eof marker.
            if (!grammar.IsAugmentedWithEofMarker)
            {
                // NOTE: If S' → S$ is not defined by the grammar then the dot symbol of the [S' → S•] item is the empty symbol,
                // and we therefore have to add eof marker 'manually' here.
                int state = dfaLr0.IndexOfUnderlyingState(itemSet => itemSet.CoreOfKernelEquals(grammar.AugmentedStartItem));
                var acceptTransition = (state, grammar.AugmentedStartItem.GetDotSymbol<Nonterminal>());
                var indexOfAcceptTransition = vertices.IndexOf(acceptTransition);
                DR[indexOfAcceptTransition].Add(grammar.Eof());
            }

            // p = 1,...,N
            foreach (int p in dfaLr0.GetTrimmedStates())
            {
                var itemSet = dfaLr0.GetUnderlyingState(p);
                foreach (var A in itemSet.NonterminalTransitions)
                {
                    var pair = (p, A);
                    var indexOfPair = vertices.IndexOf(pair);

                    // p --A--> r  -or-  GOTO(p,A) = r
                    var r = dfaLr0.TransitionFunction(pair);
                    var successorItemSet = dfaLr0.GetUnderlyingState(r);

                    foreach (var symbol in successorItemSet.Items.Where(item => !item.DotSymbol.IsEpsilon).Select(item => item.DotSymbol))
                    {
                        if (symbol.IsNonterminal)
                        {
                            if (analyzer.Erasable(symbol))
                            {
                                // indirect read: GOTO(p,AC) is defined, and C *=> ε
                                var C = (Nonterminal)symbol;
                                var successorPair = (r, C);
                                var indexOfSuccessorPair = vertices.IndexOf(successorPair);
                                edges.Add((indexOfPair, indexOfSuccessorPair));
                            }
                        }
                        else
                        {
                            // NOTE: If S' → S$ is defined then even though the LR(0) automaton (DFA) does
                            // not define [S' → S•$] --$--> [S' → S$•] (by convention) we accept it here.
                            // That is GOTO(2,$) is not defined, but DR(1,S) = {$} is ensured by the code
                            // below, because .
                            Debug.Assert(symbol.IsExtendedTerminal);
                            Debug.Assert(symbol.IsTerminal || symbol.IsEof);

                            // direct read: GOTO(p,Aa) is defined
                            var a = (Terminal<TTokenKind>) symbol; // DR(1,s) = {$} here for grammar without eof marker (by convention)
                            DR[indexOfPair].Add(a);
                        }
                    }
                }
            }

            var graph = new AdjacencyListGraph(vertices.Count, edges);

            return (ImmutableArray<IReadOnlySet<Terminal<TTokenKind>>>.CastUp(DR.ToImmutableArray()), graph);
        }

        /// <summary>
        /// Vertices are defined by all nonterminal ('goto') transitions in the LR(0) automaton denoted by a pairs on the form (p,A).
        /// We are trying to define (state-dependent, generalized) follow sets for all nonterminal transitions defined by
        ///      Follow(p,A) = {a ∈ T | S' *=> αAav, α ∈ V∗, v ∈ T* and GOTO(q0,α) = p},
        ///                  = Read(p,A) ∪ { Follow(p',B) | (p,A) includes+ (p',B) }
        ///                  = ∪ { Read(p',B) | (p,A) includes* (p',B) }
        ///
        ///      Follow(p,A) = Read(p,A) U ∪{ Follow(p',B) | (p,A) includes (p',B) }
        ///
        /// That is all singleton terminal symbols that can follow A in a sentential form whose prefix α (preceding A)
        /// accesses the state p. These are the terminals that can be shifted/read in the LR(0) automaton after the
        /// "goto transition" on nonterminal A has been performed from p.
        ///
        /// Remember a nonterminal ('goto') transition is always the final part of any reduction to A (when returning from a
        /// final item A → ω•). That is the parser must read A in state p, after returning from some state q (containing the final
        /// item [A → ω•]), and the next input symbol (aka the lookahead symbol) must be a symbol in Follow(p,A) in order for
        /// the parse to be valid (i.e. terminate successfully).
        ///
        /// direct follow sets (aka init sets)
        ///      INITFOLLOW(p,A) = Read(p,A)   (computed in previous step)
        /// indirect contributions via superset relation 'includes' (edges in digraph)
        ///      (p,A) includes (p',B)
        ///           iff
        ///      Follow(p,A) ⊇ Follow(p',B)
        ///           iff
        ///      B → βAγ, γ *=> ε and p' ∈ PRED(p,β)
        /// where
        ///      PRED(p,β) = { q | GOTO(q,β) = p },   i.e. the successor (set-valued) function used to spell-backtrack (lookback) in
        ///                                           the LR(0) automaton). The set of successor states q from where β accesses p.
        /// </summary>
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public static IGraph GetGraphLaFollow<TTokenKind, TNonterminal>(
            Grammar<TTokenKind, TNonterminal> grammar,
            LrItemsDfa<TTokenKind> dfaLr0,
            IReadOnlyOrderedSet<(int, Nonterminal)> vertices,
            IErasableSymbolsAnalyzer analyzer
            )
            where TTokenKind : struct, Enum
            where TNonterminal : struct, Enum
        {
            // includes relation defines the edges in the digraph
            var includes = new HashSet<(int, int)>(); // no parallel edges

            // B → βAγ
            // Analyser grammar: Find alle LHS med nonterminal A, hvor suffix efter A er nullable
            // Det medfoerer at Tail traverseres bagfra
            //      if Tail[n] er nonterminal
            //          edge
            //          Hvis Tail[n] er non-nullable break
            //      else
            //          break

            // We can skip S' -> S because only (1,S) is possible and (1,S) is
            // not a superset of any other pair, because it is the overall accepting
            // nonterminal transition going to the final accept state of the entire parse.
            foreach (var production in grammar.Productions.Skip(1))
            {
                var B = production.Head;
                // B → Y1Y2...Yn
                // TODO: ret follow til saadan ogsaa...better performance, because erasable does not have to be extended
                for (int i = production.Tail.Count - 1; i >= 0; i -= 1)
                {
                    var A = production.TailAs<Nonterminal>(i);
                    if (A == null) break;
                    var revBeta = production.GetSymbolsBeforeMarkerPosition(i).ToArray(); // TODO: Slice, dont't copy
                    // We have found B → βAγ, γ *=> ε

                    // TODO: It would be better to investigate the grammar, and build a marked production [B → β•Aγ]
                    //       and look it up in the Dfa. This would better resolve the correct gotoTransitionLabeledA
                    // TODO: I am not sure if the item [B → β•Aγ] will identify one or more item set (state), but if it is present
                    //       in 2 or more state then β accesses both states from the start state, and therefore both are valid subsets.
                    var gotoTransitionsLabeledA = vertices.Where(pair => A.Equals(pair.Item2)).Select(p => p.Item1).ToArray(); // TODO

                    //  For all p(i)
                    foreach (int state in gotoTransitionsLabeledA)
                    {
                        var superset = vertices.IndexOf((state, A));
                        Debug.Assert(superset >= 0);
                        // Find the set of predecessor states from where β accesses p(i)
                        // BUG: When β is _not_ the empty string, we use PRED to determine if superset is valid state-nonterminal pair
                        IEnumerable<int> predecessorStates = dfaLr0.PRED(state, revBeta);
                        // For all pred in PRED(p(i), rev(β))
                        foreach (int predecessorState in predecessorStates)
                        {
                            var subset = vertices.IndexOf((predecessorState, B));
                            // BUG: When β is the empty string, PRED will succeed for all states, but we can't be sure that the subset is a valid state-nonterminal pair
                            if (subset < 0) continue;
                            // (p(i),A) includes (pred,B)
                            includes.Add((superset, subset));
                        }
                    }

                    // γ *=> ε condition
                    if (!analyzer.Erasable(A)) break;
                }
            }

            return new AdjacencyListGraph(vertices.Count, includes);
        }

        // The LALR(1) lookahead sets are computed as
        //      LA(q, A → β) = ∪{ Follow(p,A) | (q, A → β) lookback (p,A) }
        // where
        //      (q, A → β) lookback (p,A)
        //          iff
        //      p---β--->q
        //          iff
        //      GOTO(p,β) = q
        //          iff
        //      p ∈ PRED(q,β)
        // The closure item [A → •β] in p imply that p must contain a kernel item on the form [B → α•Aβ],
        // and therefore the relation makes sense.

        /// <summary>
        /// The LALR(1) lookahead sets are computed as
        ///      LA(q, A → β) = ∪{ Follow(p,A) | (q, A → β) lookback (p,A) }
        /// where
        ///      (q, A → β) lookback (p,A)
        ///          iff
        ///      p---β--->q
        ///          iff
        ///      GOTO(p,β) = q
        ///          iff
        ///      p ∈ PRED(q,β)
        /// The closure item [A → •β] in p imply that p must contain a kernel item on the form [B → α•Aβ],
        /// and therefore the relation makes sense.
        /// </summary>
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public static Dictionary<(int stateIndex, int productionIndex), Set<Terminal<TTokenKind>>> GetLaUnion<TTokenKind, TNonterminal>(
            Grammar<TTokenKind, TNonterminal> grammar,
            LrItemsDfa<TTokenKind> dfaLr0,
            IReadOnlyOrderedSet<(int, Nonterminal)> vertices,
            IReadOnlyList<IReadOnlySet<Terminal<TTokenKind>>> followSets
            )
            where TTokenKind : struct, Enum
            where TNonterminal : struct, Enum
        {
            // TODO: Move centrally
            if (!grammar.IsAugmented)
                throw new ArgumentException("The grammar need to be augmented with S' → S starter unit production.");

            var lookaheadSets = new Dictionary<(int, int), Set<Terminal<TTokenKind>>>();

            foreach (int reduceState in dfaLr0.GetAcceptStates())
            {
                var reduceItemSet = dfaLr0.GetUnderlyingState(reduceState);
                // For all A → β• final items
                foreach (var reduceItem in reduceItemSet.ReduceItems)
                {
                    var key = (reduceState, reduceItem.ProductionIndex);
                    lookaheadSets.TryAdd(key, new Set<Terminal<TTokenKind>>());

                    // S' → S• (or S' → S•$, or S' → S$•) need special treatment, because the DFA has no transition (_, S')
                    // on S'. We can safely define it as (1, S) with regards to the eof marker follower symbol
                    if (reduceItem.Production.Head.Equals(grammar.StartSymbol))
                    {
                        // S
                        var startSymbol = reduceItem.Production.TailAs<Nonterminal>(0);
                        Debug.Assert(dfaLr0.StartState == 1);
                        // (1,S)-index
                        var index = vertices.IndexOf((dfaLr0.StartState, startSymbol));
                        // FOLLOW(1,S) = {$}
                        Debug.Assert(followSets[index].SetEquals(grammar.Eof().AsSingletonEnumerable()));
                        lookaheadSets[key].AddRange(followSets[index]);
                    }
                    else
                    {
                        var A = reduceItem.Production.Head;
                        var revBeta = reduceItem.GetRemainingSymbolsBeforeDotSymbol();
                        // for all possible predecessor states (at least one)
                        var predStates = dfaLr0.PRED(reduceState, revBeta);
                        foreach (int predState in predStates)
                        {
                            var index = vertices.IndexOf((predState, A));
                            lookaheadSets[key].AddRange(followSets[index]);
                        }
                    }
                }
            }

            // TODO: Covariance and immutable/readonly
            return lookaheadSets;
        }
    }
}
