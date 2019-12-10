using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using AutomataLib;

namespace ContextFreeGrammar.Analyzers
{
    internal class FirstSetsDigraphAnalyzer<TTokenKind> : IFirstSetsAnalyzer<TTokenKind>
        where TTokenKind : Enum
    {
        private static readonly Set<Terminal<TTokenKind>> s_eofSingleton = new Set<Terminal<TTokenKind>>(new []{Symbol.Eof<TTokenKind>()});

        private readonly Dictionary<Nonterminal, Set<Terminal<TTokenKind>>> _firstMap;

        internal FirstSetsDigraphAnalyzer(Grammar<TTokenKind> grammar, IErasableSymbolsAnalyzer analyzer)
        {
            _firstMap = ComputeFirst(grammar, analyzer);
        }

        /// <inheritdoc />
        public IReadOnlySet<Terminal<TTokenKind>> First(Symbol symbol)
        {
            if (symbol is Nonterminal variable)
                return _firstMap[variable];
            if (symbol is Terminal<TTokenKind> terminal)
                return new Set<Terminal<TTokenKind>>(new []{terminal});
            if (symbol.IsEof)
                return s_eofSingleton;
            return Set<Terminal<TTokenKind>>.Empty; // epsilon
        }

        // We can collectively characterize all FIRST sets as the smallest sets
        // FIRST(A) satisfying, for each A in N:
        //    (i)  FIRST(A) contains INITFIRST(A), and
        //    (ii) for each nonterminal B, s.t. A contains_the_first_set_of B:
        //                 FIRST(A) contains FIRST(B).
        // By the way, this is equivalent to
        //
        // FIRST(A) = INITFIRST(A) ∪ { FIRST(B) : (A,B) ∈ contains_the_first_set_of+ }
        //                            -- or --
        // FIRST(A) = ∪ { INITFIRST(B) : A contains_the_first_set_of* B }.
        //                            -- or --
        // FIRST(A) = ∪ { INITFIRST(B) : (A,B) ∈ contains_the_first_set_of* }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private Dictionary<Nonterminal, Set<Terminal<TTokenKind>>> ComputeFirst(
            Grammar<TTokenKind> grammar, IErasableSymbolsAnalyzer analyzer)
        {
            var (initFirstSets, graph) = DigraphAlgorithm.GetFirstGraph(grammar, analyzer);

            var firstSets = DigraphAlgorithm.Traverse(graph, initFirstSets);

            var firstMap = grammar.Variables.ToDictionary(v => v, v => firstSets[grammar.Variables.IndexOf(v)]);

            return firstMap;
        }

    }
}
