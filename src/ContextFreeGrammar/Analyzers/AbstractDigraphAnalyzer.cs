using System.Collections;
using System.Collections.Generic;
using AutomataLib.Graphs;

namespace ContextFreeGrammar.Analyzers
{
    // See also https://compilers.iecc.com/comparch/article/01-04-079 for sketch of algorithm
    // based on set-valued functions over digraph containing relations/edges for all set constraints
    public abstract class AbstractDigraphAnalyzer
    {
        // DFS helper that traverse the graph to determine positive transitive
        // closure (contains_the_first_set_of)+ for each terminal symbol
        protected static IEnumerable<int> Reachable(IGraph g, int start) // non-recursive, uses stack
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
