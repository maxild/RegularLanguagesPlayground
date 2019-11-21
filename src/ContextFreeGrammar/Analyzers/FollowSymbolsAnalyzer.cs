using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using AutomataLib;
using AutomataLib.Graphs;

namespace ContextFreeGrammar.Analyzers
{
    public class FollowSymbolsAnalyzer<TNonterminalSymbol, TTerminalSymbol> : DigraphAlgorithmBaseAnalyzer, IStarterSymbolsAnalyzer<TTerminalSymbol>, IFollowSymbolsAnalyzer<TNonterminalSymbol, TTerminalSymbol>
        where TTerminalSymbol : Symbol, IEquatable<TTerminalSymbol>
        where TNonterminalSymbol : Symbol, IEquatable<TNonterminalSymbol>
    {
        private readonly Dictionary<TNonterminalSymbol, Set<TTerminalSymbol>> _followMap;
        private readonly IStarterSymbolsAnalyzer<TTerminalSymbol> _analyzer;

        public FollowSymbolsAnalyzer(Grammar<TNonterminalSymbol, TTerminalSymbol> grammar, IStarterSymbolsAnalyzer<TTerminalSymbol> analyzer)
        {
            _followMap = ComputeFollow(grammar, analyzer);
            _analyzer = analyzer;
        }

        public bool Erasable(IEnumerable<Symbol> symbols) => _analyzer.Erasable(symbols);

        public bool Erasable(Symbol symbol) => _analyzer.Erasable(symbol);

        /// <inheritdoc />
        public IReadOnlySet<TTerminalSymbol> First(Symbol symbol) => _analyzer.First(symbol);

        /// <inheritdoc />
        public IReadOnlySet<TTerminalSymbol> First(IEnumerable<Symbol> symbols) => _analyzer.First(symbols);

        /// <inheritdoc />
        public IReadOnlySet<TTerminalSymbol> Follow(TNonterminalSymbol variable) => _followMap[variable];

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static Dictionary<TNonterminalSymbol, Set<TTerminalSymbol>> ComputeFollow(
            Grammar<TNonterminalSymbol, TTerminalSymbol> grammar,
            IStarterSymbolsAnalyzer<TTerminalSymbol> analyzer)
        {
            // TODO: maybe use HashSet
            var initFollowSets = grammar.Variables.ToDictionary(symbol => symbol, _ => new Set<TTerminalSymbol>());

            var contains_the_follow_set_of = new List<(int, int)>();

            // We can collectively characterize all FOLLOW sets as the smallest sets
            // FOLLOW(A) satisfying, for each A in N:
            //    (i)  FOLLOW(A) contains INITFOLLOW(A), and
            //    (ii) for each nonterminal B, s.t. A contains_the_follow_set_of B:
            //                 FOLLOW(A) contains FOLLOW(B).
            // By the way, this is equivalent to
            //
            // FOLLOW(A) = INITFOLLOW(A) ∪ { FOLLOW(B) : (A,B) ∈ contains_the_follow_set_of+ }
            //                            -- or --
            // FOLLOW(A) = ∪ { INITFOLLOW(B) : A contains_the_follow_set_of* B }.
            //                            -- or --
            // FOLLOW(A) = ∪ { INITFOLLOW(B) : (A,B) ∈ contains_the_follow_set_of* }


            // Directly Follow (direct contributions)
            // INITFOLLOW
            // INITFOLLOW(A) = { FIRST(X) | B −→ αAXβ ∈ P }, where α, β ∈ Vocabolary, and FIRST sets do not contain epsilon
            //
            // Indirect (recursive) contributions
            //    B −→ αA
            // implies
            //    FOLLOW(B) ⊆ FOLLOW(A)
            // and we define the relation
            //     A contains_the_follow_set_of B <=> FOLLOW(B) ⊆ FOLLOW(A) <=> (A,B) ∈ contains_the_follow_set_of

            foreach (var production in grammar.Productions)
            {
                // For each production X → Y1Y2...Yn
                for (int i = 0; i < production.Length; i += 1)
                {
                    // for each Yi that is a nonterminal symbol
                    var Yi = production.TailAs<TNonterminalSymbol>(i);
                    if (Yi == null) continue;
                    // Look at the current tail
                    var beta = production.Tail.Skip(i + 1).ToArray();
                    // direct contribution B −→ αAβ
                    var directlyFollowsYi =  analyzer.First(beta);
                    initFollowSets[Yi].AddRange(directlyFollowsYi);
                    // indirect contribution: B −→ αAβ and    => FOLLOW(B) ⊆ FOLLOW(A)
                    if (!Yi.Equals(production.Head) && analyzer.Erasable(beta))
                    {
                        var A = grammar.Variables.IndexOf(production.Head);
                        var B = grammar.Variables.IndexOf(Yi);
                        contains_the_follow_set_of.Add((A, B));
                    }
                }
            }

            var graph = new AdjacencyListGraph(grammar.Variables.Count, contains_the_follow_set_of);

            var followSets = grammar.Variables.ToDictionary(symbol => symbol,
                symbol => new Set<TTerminalSymbol>(initFollowSets[symbol]));

            for (int startIndex = 0; startIndex < grammar.Variables.Count; startIndex += 1)
            {
                var variable = grammar.Variables[startIndex];
                // We traverse the contains_the_follow_set_of graph in a depth-first
                // manner taken the union of every reachable FOLLOW(B) into FOLLOW(A)
                // when returning from the traversal of en edge (A,B)
                foreach (var successor in Reachable(graph, startIndex))
                {
                    var terminal = grammar.Variables[successor];
                    // FOLLOW[start] := FOLLOW[start] ∪ INITFOLLOW[successor]
                    followSets[variable].AddRange(initFollowSets[terminal]);
                }
            }

            return followSets;
        }
    }
}
