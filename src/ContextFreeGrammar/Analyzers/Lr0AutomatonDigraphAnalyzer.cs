using System;
using System.Collections.Generic;
using AutomataLib;

namespace ContextFreeGrammar.Analyzers
{
    // TODO: Make abstract LalrLookaheadSetsAnalyzer (3 metoder: digraph, dragon book, in efficient merge)
    public class Lr0AutomatonDigraphAnalyzer<TNonterminalSymbol, TTerminalSymbol>
        where TNonterminalSymbol : Symbol, IEquatable<TNonterminalSymbol>
        where TTerminalSymbol : Symbol, IEquatable<TTerminalSymbol>
    {
        private readonly Dictionary<(int, int), Set<TTerminalSymbol>> _lookaheadSets;

        public Lr0AutomatonDigraphAnalyzer(
            Grammar<TNonterminalSymbol, TTerminalSymbol> grammar,
            Dfa<ProductionItemSet<TNonterminalSymbol, TTerminalSymbol>, Symbol> dfaLr0,
            IErasableSymbolsAnalyzer analyzer)
        {
            _lookaheadSets = ComputeLookaheadSets(grammar, dfaLr0, analyzer);
        }

        // Relational formulation:
        // t ∈ LA(q, A → ω)  iff  (q, A → ω) lookback (p,A) includes* (p',B) reads* (r,C) directly-reads t
        //
        // (r,C) directly-reads t  iff  t ∈ DR(r,C)

        private static Dictionary<(int, int), Set<TTerminalSymbol>> ComputeLookaheadSets(
            Grammar<TNonterminalSymbol, TTerminalSymbol> grammar,
            Dfa<ProductionItemSet<TNonterminalSymbol, TTerminalSymbol>, Symbol> dfaLr0,
            IErasableSymbolsAnalyzer analyzer)
        {
            var vertices = LalrLookaheadSetsAlgorithm.GetGotoTransitionPairs(grammar, dfaLr0);

            var (directReads, graphRead) = LalrLookaheadSetsAlgorithm.GetGraphReads(grammar, dfaLr0, vertices, analyzer);

            // Read(p,A) sets
            Set<TTerminalSymbol>[] readSets = DigraphAlgorithm.Traverse(graphRead, directReads);

            var graphLaFollow = LalrLookaheadSetsAlgorithm.GetGraphLaFollow(grammar, dfaLr0, vertices, analyzer);

            // Follow(p,A) sets
            Set<TTerminalSymbol>[] followSets = DigraphAlgorithm.Traverse(graphLaFollow, readSets);

            // LA(q, A → ω) = ∪{ Follow(p,A) | (q, A → ω) lookback (p,A) }
            var lookaheadSets = LalrLookaheadSetsAlgorithm.GetLaUnion(grammar, dfaLr0, vertices, followSets);

            return lookaheadSets;
        }

        /// <summary>
        /// Get the set of valid LALR(1) lookahead symbols, if reducing by the given production in the given state of the LR(0) automaton (DFA).
        /// </summary>
        /// <param name="stateIndex">index according to LR(0) automaton (DFA).</param>
        /// <param name="productionIndex">The index of the production to use for the reduction.</param>
        /// <returns>The set of LALR(1) lookahead symbols when reducing by the given production in the given state.</returns>
        public IReadOnlySet<TTerminalSymbol> Lookaheads(int stateIndex, int productionIndex)
        {
            return _lookaheadSets.TryGetValue((stateIndex, productionIndex), out Set<TTerminalSymbol> lookaheads)
                ? lookaheads
                : Set<TTerminalSymbol>.Empty;
        }

        public int CountOfLookaheads => _lookaheadSets.Count;
    }
}