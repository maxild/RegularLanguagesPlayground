using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using AutomataLib;
using ContextFreeGrammar.Analyzers.Internal;

namespace ContextFreeGrammar.Analyzers
{
    internal class FollowSetsDigraphAnalyzer<TNonterminalSymbol, TTerminalSymbol> : IFollowSetsAnalyzer<TNonterminalSymbol, TTerminalSymbol>
        where TTerminalSymbol : Symbol, IEquatable<TTerminalSymbol>
        where TNonterminalSymbol : Symbol, IEquatable<TNonterminalSymbol>
    {
        private readonly Dictionary<TNonterminalSymbol, Set<TTerminalSymbol>> _followMap;

        internal FollowSetsDigraphAnalyzer(
            Grammar<TNonterminalSymbol, TTerminalSymbol> grammar,
            IErasableSymbolsAnalyzer nullableSymbolsAnalyzer,
            IFirstSetsAnalyzer<TTerminalSymbol> starterTokensAnalyzer)
        {
            var analyzer = new FirstSymbolsAnalyzer<TTerminalSymbol>(nullableSymbolsAnalyzer, starterTokensAnalyzer);
            _followMap = ComputeFollow(grammar, analyzer);
        }

        /// <inheritdoc />
        public IReadOnlySet<TTerminalSymbol> Follow(TNonterminalSymbol variable) => _followMap[variable];

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static Dictionary<TNonterminalSymbol, Set<TTerminalSymbol>> ComputeFollow(
            Grammar<TNonterminalSymbol, TTerminalSymbol> grammar,
            IFirstSymbolsAnalyzer<TTerminalSymbol> analyzer)
        {
            var (initFollowSets, graph) = DigraphAlgorithm.GetFollowGraph(grammar, analyzer);

            var followSets = DigraphAlgorithm.Traverse(grammar, graph, initFollowSets);

            var followMap = grammar.Variables.ToDictionary(v => v, v => followSets[grammar.Variables.IndexOf(v)]);

            return followMap;
        }
    }
}
