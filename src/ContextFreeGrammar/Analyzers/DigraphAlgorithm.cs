using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using AutomataLib;
using AutomataLib.Graphs;

namespace ContextFreeGrammar.Analyzers
{
    // See also https://compilers.iecc.com/comparch/article/01-04-079 for sketch of algorithm
    // based on set-valued functions over digraph containing relations/edges for all set constraints
    public static class DigraphAlgorithm
    {
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public static (Set<TTerminalSymbol>[] INITFIRST, IGraph Graph) GetFirstGraph<TNonterminalSymbol, TTerminalSymbol>(
            Grammar<TNonterminalSymbol, TTerminalSymbol> grammar, IErasableSymbolsAnalyzer analyzer)
            where TTerminalSymbol : Symbol, IEquatable<TTerminalSymbol>
            where TNonterminalSymbol : Symbol, IEquatable<TNonterminalSymbol>
        {
            // direct contributions: initial first sets (INITFIRST)
            var initSets = new Set<TTerminalSymbol>[grammar.Variables.Count];
            for (int i = 0; i < initSets.Length; i += 1)
                initSets[i] = new Set<TTerminalSymbol>();

            // indirect contributions: superset relations between nonterminals
            var contains_the_first_set_of = new HashSet<(int, int)>(); // no parallel edges

            // For each production A → Y1Y2...Yn
            foreach (var production in grammar.Productions)
            {
                var Aix = grammar.Variables.IndexOf(production.Head);

                foreach (var Yi in production.Tail)
                {
                    // direct contribution: A → αaβ, where α *=> ε and a ∈ T
                    if (Yi is TTerminalSymbol a)
                        initSets[Aix].Add(a); // we break below when seeing terminal symbol

                    // indirect (recursive) contribution (ignoring self-loops):
                    //       A → αBβ, where α *=> ε and B ∈ N, and B ≠ A
                    if (Yi is TNonterminalSymbol B)
                    {
                        var Bix = grammar.Variables.IndexOf(B);
                        if (Aix != Bix)
                            contains_the_first_set_of.Add((Aix, Bix));
                    }

                    // if we cannot erase Yi (that is Yi *=> ε is not possible),
                    // then this production cannot yield any more contributions.
                    if (!analyzer.Erasable(Yi)) break;
                }
            }

            var graph = new AdjacencyListGraph(grammar.Variables.Count, contains_the_first_set_of);

            return (initSets, graph);
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public static (Set<TTerminalSymbol>[] INITFOLLOW, IGraph Graph) GetFollowGraph<TNonterminalSymbol,
            TTerminalSymbol>(
            Grammar<TNonterminalSymbol, TTerminalSymbol> grammar, IFirstSymbolsAnalyzer<TTerminalSymbol> analyzer)
            where TTerminalSymbol : Symbol, IEquatable<TTerminalSymbol>
            where TNonterminalSymbol : Symbol, IEquatable<TNonterminalSymbol>
        {
            // direct contributions: initial follow sets (INITFOLLOW)
            var initSets = new Set<TTerminalSymbol>[grammar.Variables.Count];
            for (int i = 0; i < initSets.Length; i += 1)
                initSets[i] = new Set<TTerminalSymbol>();

            // indirect contributions: superset relations between nonterminals
            var contains_the_follow_set_of = new HashSet<(int, int)>(); // parallel edges

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

            // For each production B → Y1Y2...Yn
            foreach (var production in grammar.Productions)
            {
                var Bix = grammar.Variables.IndexOf(production.Head);

                for (int i = 0; i < production.Length; i += 1)
                {
                    // for each Yi that is a nonterminal symbol
                    var Yi = production.TailAs<TNonterminalSymbol>(i);
                    if (Yi == null) continue;
                    // Look at the current tail
                    var beta = production.Tail.Skip(i + 1).ToArray();
                    // direct contribution B −→ αAβ
                    var Aix = grammar.Variables.IndexOf(Yi);
                    // FIRST(β) ⊆ INITFOLLOW(A)
                    var directlyFollowsYi = analyzer.First(beta);
                    initSets[Aix].AddRange(directlyFollowsYi);
                    // indirect contribution: B −→ αAβ and β *=> ε, and B ≠ A => FOLLOW(B) ⊆ FOLLOW(A)
                    if (Aix != Bix && analyzer.Erasable(beta))
                        contains_the_follow_set_of.Add((Aix, Bix));
                }
            }

            var graph = new AdjacencyListGraph(grammar.Variables.Count, contains_the_follow_set_of);

            return (initSets, graph);
        }

        // TODO: This simple DFS traversal can be optimized using Component Graph (SCCs)
        public static Set<TResult>[] Traverse<TNonterminalSymbol, TTerminalSymbol, TResult>(
            Grammar<TNonterminalSymbol, TTerminalSymbol> grammar,
            IGraph graph,
            Set<TResult>[] initSets)
        where TNonterminalSymbol : Symbol, IEquatable<TNonterminalSymbol>
        where TTerminalSymbol : Symbol, IEquatable<TTerminalSymbol>
        where TResult : IEquatable<TResult>
        {
            int count = grammar.Variables.Count;

            Debug.Assert(count == initSets.Length);

            // copy result (typically terminals) from init sets to the set-valued functions before traversing
            var f = new Set<TResult>[count];
            for (int i = 0; i < count; i += 1)
                f[i] = new Set<TResult>(initSets[i]);

            for (int start = 0; start < count; start += 1)
            {
                // We traverse the graph induced by all the superset relations in
                // a depth-first manner, taking the union of every reachable F(B)
                // into F(A) when returning from the traversal of en edge (A,B)
                foreach (var successor in Reachable(graph, start))
                {
                    // F[start] := F[start] ∪ F0[successor]
                    f[start].AddRange(initSets[successor]);
                }
            }

            return f;

            // DFS helper that traverse the graph to determine positive transitive
            // closure (contains_the_first_set_of)+ for each terminal symbol
            static IEnumerable<int> Reachable(IGraph g, int start) // non-recursive, uses stack
            {
                var visited = new BitArray(g.VertexCount);
                var stack = new Stack<int>();

                stack.Push(start);

                var reachable = new List<int>();

                while (stack.Count > 0)
                {
                    var current = stack.Pop();
                    visited[current] = true;
                    foreach (var successor in g.NeighboursOf(current))
                    {
                        if (!visited[successor])
                        {
                            stack.Push(successor);
                            reachable.Add(successor);
                        }
                    }
                }

                return reachable;
            }
        }
    }
}
