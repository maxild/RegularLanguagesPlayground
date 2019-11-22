using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace AutomataLib.Graphs
{
    // TVertex = int, char, string
    public interface IGraph
    {
        int VertexCount { get; }

        int EdgeCount { get; }

        IEnumerable<int> Vertices { get; }

        IEnumerable<(int,int)> Edges { get; }

        IEnumerable<int> NeighboursOf(Index vertex);
    }

    // C# 8 Range and Index (slicing and indexing syntactic sugar)
    //public interface Ix<T> // Ix class in Haskell
    //{
    //    IEnumerable<T> Range
    //}

    // vertices are indexed [0,1,..,n[ (Ix class in haskell)
    // edges are labeled? no
    // directed by default? yes
    // TODO: Use the vocabolary of CLRS book (vertex, vertices, edge, edges, inDegree, outDegree, etc...)
    // That is we cannot use Move, Transition for edge in DFA/NFA models
    public class AdjacencyListGraph : IGraph
    {
        // array of adjacency lists
        private readonly int[][] _arrayOfNeighbours;

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

            _arrayOfNeighbours = new int[numberOfVertices][];
            for (int i = 0; i < numberOfVertices; i++)
                _arrayOfNeighbours[i] = adjList[i].ToArray(); // could be sorted, and use BinarySearch in ContainsEdge
        }

        public int VertexCount => _arrayOfNeighbours.Length;

        public int EdgeCount => _arrayOfNeighbours.Sum(al => al.Length);

        public IEnumerable<int> Vertices => Enumerable.Range(0, VertexCount);

        public IEnumerable<(int, int)> Edges
        {
            get
            {
                for (int i = 0; i < _arrayOfNeighbours.Length; i += 1)
                    for (int j = 0; j < _arrayOfNeighbours[i].Length; j += 1)
                        yield return (i, _arrayOfNeighbours[i][j]);
            }
        }

        public IEnumerable<int> NeighboursOf(Index vertex)
        {
            return _arrayOfNeighbours[vertex];
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
                foreach (var succ in graph.NeighboursOf(curr))
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
