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
        private readonly Grammar<TNonterminalSymbol, TTerminalSymbol> _grammar;
        private readonly Dictionary<(int, MarkedProduction<TNonterminalSymbol>), Set<TTerminalSymbol>> _lookaheadSets;

        public Lr0AutomatonDigraphAnalyzer(
            Grammar<TNonterminalSymbol, TTerminalSymbol> grammar,
            Dfa<ProductionItemSet<TNonterminalSymbol, TTerminalSymbol>, Symbol> dfaLr0,
            IErasableSymbolsAnalyzer analyzer)
        {
            _lookaheadSets = ComputeLookaheadSets(grammar, dfaLr0, analyzer);
            _grammar = grammar;
        }

        // Relational formulation:
        // t ∈ LA(q, A → ω)  iff  (q, A → ω) lookback (p,A) includes* (p',B) reads* (r,C) directly-reads t
        //
        // (r,C) directly-reads t  iff  t ∈ DR(r,C)

        private static Dictionary<(int, MarkedProduction<TNonterminalSymbol>), Set<TTerminalSymbol>> ComputeLookaheadSets(
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

        public IEnumerable<TTerminalSymbol> Lookaheads(int reduceState, int productionIndex)
        {
            var production = _grammar.Productions[productionIndex];
            var reduceItem = new MarkedProduction<TNonterminalSymbol>(production, productionIndex, production.Tail.Count);
            return _lookaheadSets.TryGetValue((reduceState, reduceItem), out Set<TTerminalSymbol> lookaheads)
                ? lookaheads
                : Set<TTerminalSymbol>.Empty;
        }
    }
}
