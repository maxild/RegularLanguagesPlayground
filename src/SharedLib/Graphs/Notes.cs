// G = (V, E), where |V| = n and |E| = m (n nodes, m edges)
//
// For undirected graph the edges are unordered pairs.

// For directed graph (digraph) the edges are ordered pairs. An edge (i,j) is directed from i to j,
// where i is called the source of the edge and j is called the target.
//
// We will often assume that V={0,...,n-1}. Any other data that we would like to associate with the
// elements of V can be stored in an array of length n.
//
// Some typical operations performed on graphs are:
//
//      * addEdge(i,j): Add the edge (i, j) to E.
//      * removeEdge(i,j): Remove the edge (i, j) from E.
//      * hasEdge(i,j): Check if the edge (i,j) is in E
//      * outEdges(i): Return a List of all integers j such that (i, j) is in E
//      * inEdges(i): Return a List of all integers j such that (j, i) is in E
//
// Example
// As an example we might have a graph with four vertices/nodes (A, B, C, and D) and
// four edges: (A,B), (B,C), (C,A), (C,D).
//
// Representations
// ===============
// 1. Object oriented representation
//      Have some structure/object for each vertex/node (representing whatever information you want to store there),
//      and another structure for each edge (with pointers/references to the two vertices/nodes it connects).
//      This representation is a little difficult to work with, because unless the edges are ordered more carefully
//      it will be difficult to find the ones you want
//      The object oriented representation would just have 2 lists (arrays) of node and edge objects.
//          nodes = {A,B,C,D}
//          edges = {(A,B), (B,C), (C,A), (C,D)}
//      where A,B,C,D represent object references.
//
//      NOTE: This is the representation I often use when translating between representations, or when initializing
//            any given representation. It is not a good representation for any other things (think algorithms).
//
//      The total space used by this representation is just O(m+n), since there is a constant amount of space (one structure)
//      per vertex or edge. Most operations in this representation involve scanning the whole list of edges, and take time O(m).
//
// 2. Adjacency list (See also http://opendatastructures.org/versions/edition-0.1e/ods-java/12_2_AdjacencyLists_Graph_a.html)
//      In the adjacency list representation, each vertex keeps a linked list (or list, array) of the neighboring (adjacent)
//      vertices. The edges don't really appear at all.
//      The lists for each vertex would be (this is for an undirected graph):
//           A: {B, C}
//           B: {A, C}
//           C: {D, B, A}
//           D: {C}
//      This representation makes it much easier to find the edges connected to any particular vertex. Its space is still also
//      small: O(m+n), since the total length of all the lists is 2m (each edge appears twice, once for each of its endpoints).
//      It is also quite fast for many applications. The slowest operation that might commonly be used is testing whether a pair
//      of vertices is connected by an edge; this has to be done by scanning through one of the lists, but you could speed it up
//      by sorting the adjacency lists and using binary search. Another disadvantage is that each edge is listed twice, so if the
//      edges carry any extra information such as a length it may be complicated to keep track of both copies and make sure they
//      have the same information.
//
// 3. Incidence list
//      By combining the adjacency list and the object oriented representation, we get something with the advantages of both. We
//      just add to the object oriented representation a list, for each vertex, of pointers to the edges incident to it. If I don't
//      specify a representation, this is probably what I have in mind. The space is a little larger than the previous two representations,
//      but still O(m+n).
//          nodes_with_adjacency_list = {(A, {B,C}),(B, {A,C}),(C, {D,B,A}),(D, {C})}
//          edges = {(A,B), (C,D), (B,C), (C,A)}     (unordered pairs for undirected graph, i.e. (A,B)=(B,A))
// 4. Adjacency matrix (See also http://opendatastructures.org/versions/edition-0.1e/ods-java/12_1_AdjacencyMatrix_Repres.html)
//      In some situations we are willing to use a somewhat larger data structure so that we can test very quickly whether an edge exists.
//      We make an (n x n) matrix M[i,j], with rows and columns indexed by vertices. If edge (u,v) is present, we put a one in cell M[u,v];
//      otherwise we leave M[u,v] zero. Finding the neighbors of a vertex involves scanning a row in O(n) time, but to test if an edge (u,v)
//      exists just look in that entry, in constant time. To store extra information like edge lengths, you can just use more matrices.
//
//      For the same graph above, we'd have a matrix
//          0 1 1 0
//          1 0 1 0
//          1 1 0 1
//          0 0 1 0
//      For an undirected graph, the matrix will be symmetric, because edges are a unordered pairs such that M(i,j)=M(j,i) for all i,j (about
//      half the storage is redundant)
//
// 5. Incidence matrix
//      This is another matrix, but generally it is rectangular rather than square; Its dimension is (n x m), because the rows are indexed by
//      vertices and the columns by edges. Just like the adjacency matrix, we put a one in a cell when the corresponding vertex and edge are
//      incident. Therefore every column will have exactly two ones in it. For a directed graph, you can make a similar matrix in which every
//      column has one +1 and one -1 entry. This matrix is usually not symmetric.
//
//      For the same graph, the incidence matrix is
//          1 0 1 0
//          1 1 0 0
//          0 1 1 1
//          0 0 0 1
//      NOTE: edges must be stored in indexed/ordered set (A,B), (B,C), (C,A), (C,D)
//
// Links to be examined
//      - https://www.ics.uci.edu/~eppstein/161/syl.html and https://www.ics.uci.edu/~eppstein/161/960201.html
//      - http://opendatastructures.org/versions/edition-0.1e/ods-java/12_Graphs.html
//      - https://www.boost.org/doc/libs/1_70_0/libs/graph/doc/index.html
//      - https://www.codeproject.com/Articles/5603/QuickGraph-A-100-C-graph-library-with-Graphviz-Sup
//      - https://archive.codeplex.com/?p=quickgraph
//      - https://github.com/Microsoft/automatic-graph-layout
//      - https://github.com/auduchinok/DotParser/blob/master/README.md
//      - https://github.com/etmendz/Mendz.Graph
//      - https://github.com/panthernet/GraphX/blob/PCL/README.md
//      - https://github.com/mokeyish/QuickGraph and https://github.com/YaccConstructor/QuickGraph
//
