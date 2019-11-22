using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using AutomataLib;

namespace ContextFreeGrammar.Analyzers
{
    public class FirstSetsDigraphAnalyzer<TNonterminalSymbol, TTerminalSymbol> : AbstractDigraphAnalyzer, IFirstSetsAnalyzer<TTerminalSymbol>
        where TTerminalSymbol : Symbol, IEquatable<TTerminalSymbol>
        where TNonterminalSymbol : Symbol, IEquatable<TNonterminalSymbol>

    {
        private static readonly Set<TTerminalSymbol> s_eofSingleton = new Set<TTerminalSymbol>(new []{Symbol.Eof<TTerminalSymbol>()});
        // TODO: Set.Empty<T>()
        private static readonly Set<TTerminalSymbol> s_empty = new Set<TTerminalSymbol>();

        private readonly Dictionary<TNonterminalSymbol, Set<TTerminalSymbol>> _firstMap;

        public FirstSetsDigraphAnalyzer(Grammar<TNonterminalSymbol, TTerminalSymbol> grammar, IErasableSymbolsAnalyzer analyzer)
        {
            _firstMap = ComputeFirst(grammar, analyzer);
        }

        /// <inheritdoc />
        public IReadOnlySet<TTerminalSymbol> First(Symbol symbol)
        {
            if (symbol is TNonterminalSymbol variable)
                return _firstMap[variable];
            if (symbol is TTerminalSymbol token)
                return new Set<TTerminalSymbol>(new []{token});
            if (symbol.IsEof)
                return s_eofSingleton;
            return s_empty; // epsilon
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private Dictionary<TNonterminalSymbol, Set<TTerminalSymbol>> ComputeFirst(
            Grammar<TNonterminalSymbol, TTerminalSymbol> grammar, IErasableSymbolsAnalyzer analyzer)
        {
            var (initFirstSets, graph) = DigraphAlgorithm.GetFirstGraph(grammar, analyzer);

            var firstSets = new Set<TTerminalSymbol>[initFirstSets.Length];
            for (int i = 0; i < initFirstSets.Length; i += 1)
                firstSets[i] = new Set<TTerminalSymbol>(initFirstSets[i]); // copy

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
            // TODO: This simple DFS traversal can be optimized using Component Graph (SCCs)
            for (int start = 0; start < grammar.Variables.Count; start += 1)
            {
                // We traverse the contains_the_first_set_of graph in a depth-first
                // manner taken the union of every reachable FIRST(B) into FIRST(A)
                // when returning from the traversal of en edge (A,B)
                foreach (var successor in Reachable(graph, start))
                {
                    // FIRST[start] := FIRST[start] ∪ INITFIRST[successor]
                    firstSets[start].AddRange(initFirstSets[successor]);
                }
            }

            var firstMap = grammar.Variables.ToDictionary(symbol => symbol,
                symbol => firstSets[grammar.Variables.IndexOf(symbol)]);

            return firstMap;
        }
    }
}
