using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using AutomataLib;
using AutomataLib.Graphs;

namespace ContextFreeGrammar.Analyzers
{
    public class FirstSymbolsAnalyzer<TNonterminalSymbol, TTerminalSymbol> : DigraphAlgorithmBaseAnalyzer, IStarterSymbolsAnalyzer<TTerminalSymbol>
        where TTerminalSymbol : Symbol, IEquatable<TTerminalSymbol>
        where TNonterminalSymbol : Symbol, IEquatable<TNonterminalSymbol>

    {
        private readonly Dictionary<TNonterminalSymbol, Set<TTerminalSymbol>> _firstMap;
        private readonly IErasableSymbolsAnalyzer _analyzer;

        public FirstSymbolsAnalyzer(Grammar<TNonterminalSymbol, TTerminalSymbol> grammar, IErasableSymbolsAnalyzer analyzer)
        {
            _firstMap = ComputeFirst(grammar, analyzer);
            _analyzer = analyzer;
        }

        public bool Erasable(IEnumerable<Symbol> symbols) => _analyzer.Erasable(symbols);

        public bool Erasable(Symbol symbol) => _analyzer.Erasable(symbol);

        /// <inheritdoc />
        public IReadOnlySet<TTerminalSymbol> First(Symbol symbol)
        {
            if (symbol is TNonterminalSymbol variable)
                return _firstMap[variable];
            if (symbol is TTerminalSymbol token)
                return new Set<TTerminalSymbol>(new []{token});
            // TODO: Set.Empty<T>()
            return new Set<TTerminalSymbol>(); // epsilon, eof
        }

        /// <inheritdoc />
        public IReadOnlySet<TTerminalSymbol> First(IEnumerable<Symbol> symbols)
        {
            var m = new Set<TTerminalSymbol>();
            foreach (var symbol in symbols)
            {
                m.AddRange(First(symbol));
                if (!_analyzer.Erasable(symbol)) break;
            }
            return m;
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private Dictionary<TNonterminalSymbol, Set<TTerminalSymbol>> ComputeFirst(
            Grammar<TNonterminalSymbol, TTerminalSymbol> grammar, IErasableSymbolsAnalyzer analyzer)
        {
            // TODO: INITFIRST sets should be indexed by terminal index
            // TODO: production.HeadIndex is not possible without getting grammar to resolve it: Grammar.Variables.IndexOf(production.Head)
            // direct/initial first sets
            var initFirstSets =
                grammar.Variables.ToDictionary(symbol => symbol, _ => new Set<TTerminalSymbol>()); // maybe use HashSet

            // superset relations between nonterminals
            var contains_the_first_set_of = new List<(int, int)>();

            // initialize initial first sets and list of relations
            foreach (var production in grammar.Productions)
            {
                foreach (var Yi in production.Tail)
                {
                    if (Yi is TTerminalSymbol a)
                    {
                        // direct contribution: A → αaβ, where α *=> ε and a ∈ T
                        initFirstSets[production.Head].Add(a);
                        break;
                    }

                    if (Yi is TNonterminalSymbol B)
                    {
                        // indirect (recursive) contribution: A → αBβ, where α *=> ε and B ∈ N
                        var Ai = grammar.Variables.IndexOf(production.Head);
                        var Bi = grammar.Variables.IndexOf(B);
                        contains_the_first_set_of.Add((Ai, Bi));

                        // if we cannot erase Yi (Yi *=> ε), then no more 'direct contributions' or
                        // subset (contains_the_first_set_of) relation pairs exist for this production rule
                        if (!analyzer.Erasable(B)) break;
                    }
                }
            }

            var graph = new AdjacencyListGraph(grammar.Variables.Count, contains_the_first_set_of);

            var firstSets = grammar.Variables.ToDictionary(symbol => symbol,
                symbol => new Set<TTerminalSymbol>(initFirstSets[symbol]));

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
            for (int startIndex = 0; startIndex < grammar.Variables.Count; startIndex += 1)
            {
                var variable = grammar.Variables[startIndex];
                // We traverse the contains_the_first_set_of graph in a depth-first
                // manner taken the union of every reachable FIRST(B) into FIRST(A)
                // when returning from the traversal of en edge (A,B)
                foreach (var successor in Reachable(graph, startIndex))
                {
                    var terminal = grammar.Variables[successor];
                    // FIRST[start] := FIRST[start] ∪ INITFIRST[successor]
                    firstSets[variable].AddRange(initFirstSets[terminal]);
                }
            }

            return firstSets;
        }
    }
}
