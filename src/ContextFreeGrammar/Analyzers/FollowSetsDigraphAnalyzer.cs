using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using AutomataLib;
using ContextFreeGrammar.Analyzers.Internal;

namespace ContextFreeGrammar.Analyzers
{
    internal class FollowSetsDigraphAnalyzer<TTokenKind> : IFollowSetsAnalyzer<TTokenKind>
        where TTokenKind : struct, Enum
    {
        private readonly Dictionary<Nonterminal, Set<Terminal<TTokenKind>>> _followMap;

        internal FollowSetsDigraphAnalyzer(
            Grammar<TTokenKind> grammar,
            IErasableSymbolsAnalyzer nullableSymbolsAnalyzer,
            IFirstSetsAnalyzer<TTokenKind> starterTokensAnalyzer)
        {
            var analyzer = new FirstSymbolsAnalyzer<TTokenKind>(nullableSymbolsAnalyzer, starterTokensAnalyzer);
            _followMap = ComputeFollow(grammar, analyzer);
        }

        /// <inheritdoc />
        public IReadOnlySet<Terminal<TTokenKind>> Follow(Nonterminal variable) => _followMap[variable];

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static Dictionary<Nonterminal, Set<Terminal<TTokenKind>>> ComputeFollow(
            Grammar<TTokenKind> grammar,
            IFirstSymbolsAnalyzer<TTokenKind> analyzer)
        {
            var (initFollowSets, graph) = DigraphAlgorithm.GetFollowGraph(grammar, analyzer);

            var followSets = DigraphAlgorithm.Traverse(graph, initFollowSets);

            var followMap = grammar.Nonterminals.ToDictionary(v => v, v => followSets[grammar.Nonterminals.IndexOf(v)]);

            return followMap;
        }
    }
}
