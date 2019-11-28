using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace AutomataLib.Graphs
{
    public interface IGraph
    {
        int VertexCount { get; }

        int EdgeCount { get; }

        IEnumerable<int> Vertices { get; }

        IEnumerable<(int,int)> Edges { get; }

        IEnumerable<int> NeighborsOf(Index vertex);
    }

    public class AdjacencyListGraph : IGraph
    {
        // array of adjacency lists
        private readonly int[][] _arrayOfNeighbors;

        // TODO: Could be Range as type of first arg (keep naming of vertices out of the Graph type..make naming abstraction)
        public AdjacencyListGraph(int numberOfVertices, IEnumerable<(int,int)> edges)
        {
            var adjList = new List<int>[numberOfVertices];
            for (int i = 0; i < numberOfVertices; i++)
                adjList[i] = new List<int>();

            foreach (var edge in edges)
            {
                adjList[edge.Item1].Add(edge.Item2); // TODO: avoid duplicates (parallel edges)
            }

            _arrayOfNeighbors = new int[numberOfVertices][];
            for (int i = 0; i < numberOfVertices; i++)
                _arrayOfNeighbors[i] = adjList[i].ToArray(); // could be sorted, and use BinarySearch in ContainsEdge
        }

        public int VertexCount => _arrayOfNeighbors.Length;

        public int EdgeCount => _arrayOfNeighbors.Sum(al => al.Length);

        public IEnumerable<int> Vertices => Enumerable.Range(0, VertexCount);

        public IEnumerable<(int, int)> Edges
        {
            get
            {
                for (int i = 0; i < _arrayOfNeighbors.Length; i += 1)
                    for (int j = 0; j < _arrayOfNeighbors[i].Length; j += 1)
                        yield return (i, _arrayOfNeighbors[i][j]);
            }
        }

        public IEnumerable<int> NeighborsOf(Index vertex)
        {
            return _arrayOfNeighbors[vertex];
        }
    }

    public static class GraphTraversal
    {
        // The result is a depth-first result set 'tree'
        //    with nothing => no structure (could be any traversal, BFS perhaps)
        //    with predecessor (parent) => tree
        // DFS specific result data
        //    with timestamp (traversalIndex) => (topological) sort    (NOTE: BFS has distance/layer as specific data)
        public static IEnumerable<int> DepthFirstSearch(IGraph graph, int start) // non-recursive, uses stack
        {
            var visited = new BitArray(graph.VertexCount);
            var stack = new Stack<int>();

            stack.Push(start);

            var reachable = new List<int>();

            while (stack.Count > 0)
            {
                var curr = stack.Pop();
                visited[curr] = true;
                foreach (var succ in graph.NeighborsOf(curr))
                {
                    if (!visited[succ])
                    {
                        stack.Push(succ);
                        // Add set of succ to
                        //    F[start] := F[start] ∪ I[succ]
                        reachable.Add(succ);
                    }
                }
            }

            return reachable;

            // Loop and inner (see notes)

            // TODO: What is output
            //     depth-first tree (forests)
            //     topological sort (dependencies)
            //     reachable vertices (reflexive, transitive closure)
            //     SCCs (pieces)
        }
    }
}
