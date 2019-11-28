using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        public static IReadOnlyOrderedSet<(int, TNonterminalSymbol)> GetGotoTransitionPairs<TNonterminalSymbol, TTerminalSymbol>(
            Grammar<TNonterminalSymbol, TTerminalSymbol> grammar,
            Dfa<ProductionItemSet<TNonterminalSymbol, TTerminalSymbol>, Symbol> dfaLr0)
                where TTerminalSymbol : Symbol, IEquatable<TTerminalSymbol>
                where TNonterminalSymbol : Symbol, IEquatable<TNonterminalSymbol>
        {
            // This is the bijective mapping between integer values and (p,A)-pairs.
            var vertices = new InsertionOrderedSet<(int, TNonterminalSymbol)>();

            // Ensure that for the state p whose kernel item is [S' → S•], we have
            //       DR(p, S) = {$},
            // _even_ if the grammar haven't been augmented with an eof marker.
            if (!grammar.IsAugmentedWithEofMarker)
            {
                // TODO: Not DRY
                int state = dfaLr0.IndexOfUnderlyingState(itemSet => itemSet.ContainsKernelItem(grammar.AugmentedStartItem));
                var acceptTransition = (state, grammar.AugmentedStartItem.GetDotSymbol<TNonterminalSymbol>());
                vertices.Add(acceptTransition);
            }

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
        ///                = DR(p,A) ∪ { Read(r,C) : (p,A) reads+ (p,C) }
        ///                = ∪ { DR(r,C) : (p,A) reads* (p,C) }
        /// That is all singleton terminal symbols that can follow any nonterminal ('goto') transition (p,A). Remember a
        /// nonterminal ('goto') transition is always the final part of any reduction to A (when returning from a final item A → ω•).
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
        public static (ImmutableArray<IReadOnlySet<TTerminalSymbol>> DR, IGraph Graph) GetGraphReads<TNonterminalSymbol, TTerminalSymbol>(
            Grammar<TNonterminalSymbol, TTerminalSymbol> grammar,
            Dfa<ProductionItemSet<TNonterminalSymbol, TTerminalSymbol>, Symbol> dfaLr0,
            IReadOnlyOrderedSet<(int, TNonterminalSymbol)> vertices,
            IErasableSymbolsAnalyzer analyzer)
            where TTerminalSymbol : Symbol, IEquatable<TTerminalSymbol>
            where TNonterminalSymbol : Symbol, IEquatable<TNonterminalSymbol>
        {
            // TODO: Move everywhere (maybe an invariant of grammar itself...If (!IsAugmented) MakeAugmented
            if (!grammar.IsAugmented)
                throw new ArgumentException(
                    "The first production of the grammar must be a unit production on the form S' → S, where S' is found no where else in any productions ");

            var edges = new HashSet<(int, int)>(); // no parallel edges

            // direct reads (init sets)
            var DR = new Set<TTerminalSymbol>[vertices.Count]; // terminals, including eof = $
            for (int i = 0; i < vertices.Count; i += 1)
                DR[i] = new Set<TTerminalSymbol>();

            // Ensure that for the state p whose kernel item is [S' → S•], we have
            //       DR(p, S) = {$},
            // _even_ if the grammar haven't been augmented with an eof marker.
            if (!grammar.IsAugmentedWithEofMarker)
            {
                int state = dfaLr0.IndexOfUnderlyingState(itemSet => itemSet.ContainsKernelItem(grammar.AugmentedStartItem));
                var acceptTransition = (state, grammar.AugmentedStartItem.GetDotSymbol<TNonterminalSymbol>());
                var indexOfAcceptTransition = vertices.IndexOf(acceptTransition);
                DR[indexOfAcceptTransition].Add(Symbol.Eof<TTerminalSymbol>());
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
                        if (symbol.IsNonTerminal)
                        {
                            if (analyzer.Erasable(symbol))
                            {
                                // indirect read: GOTO(p,AC) is defined, and C *=> ε
                                var C = (TNonterminalSymbol)symbol;
                                var successorPair = (r, C);
                                var indexOfSuccessorPair = vertices.IndexOf(successorPair);
                                edges.Add((indexOfPair, indexOfSuccessorPair));
                            }
                        }
                        else
                        {
                            // direct read: GOTO(p,Aa) is defined
                            var a = (TTerminalSymbol)symbol;
                            DR[indexOfPair].Add(a);
                        }
                    }
                }
            }

            var graph = new AdjacencyListGraph(vertices.Count, edges);

            return (ImmutableArray<IReadOnlySet<TTerminalSymbol>>.CastUp(DR.ToImmutableArray()), graph);
        }
    }
}
