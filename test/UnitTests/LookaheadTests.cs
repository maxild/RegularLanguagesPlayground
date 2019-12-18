using System.Collections.Generic;
using AutomataLib;
using ContextFreeGrammar;
using ContextFreeGrammar.Analyzers;
using GrammarRepo;
using Shouldly;
using Xunit;
using Sym = GrammarRepo.DragonBookExample4_48.Sym;
using Var = GrammarRepo.DragonBookExample4_48.Var;

namespace UnitTests
{
    public class LookaheadTests
    {
        public LookaheadTests()
        {
            // 0: S' → S     (the digraph algorithms will augment the unit production with eof marker)
            // 1: S  → L = R
            // 2: S  → R
            // 3: R  → *R
            // 4: R  → a     ('id' in Gallier notes)
            // 5: R  → L
            Grammar = DragonBookExample4_48.GetGrammar();

            DfaLr0 = Grammar.GetLr0AutomatonDfa();

            // 0: S' → S$    (the digraph algorithms will augment the unit production with eof marker)
            // 1: S  → L = R
            // 2: S  → R
            // 3: R  → *R
            // 4: R  → a     ('id' in Gallier notes)
            // 5: R  → L
            GrammarEof = DragonBookExample4_48.GetExtendedGrammar();

            DfaLr0Eof = GrammarEof.GetLr0AutomatonDfa();
        }

        private Grammar<Sym, Var> Grammar { get; }

        private Grammar<Sym, Var> GrammarEof { get; }

        private LrItemsDfa<Sym> DfaLr0 { get; }

        private LrItemsDfa<Sym> DfaLr0Eof { get; }

        [Fact]
        public void Vertices()
        {
            foreach (var vertices in new []
            {
                LalrLookaheadSetsAlgorithm.GetGotoTransitionPairs(Grammar, DfaLr0),
                LalrLookaheadSetsAlgorithm.GetGotoTransitionPairs(GrammarEof, DfaLr0Eof)
            })
            {
                vertices.IndexOf((1, Grammar.V(Var.S))).ShouldBeGreaterThanOrEqualTo(0);
                vertices.IndexOf((1, Grammar.V(Var.R))).ShouldBeGreaterThanOrEqualTo(0);
                vertices.IndexOf((1, Grammar.V(Var.L))).ShouldBeGreaterThanOrEqualTo(0);
                vertices.IndexOf((5, Grammar.V(Var.R))).ShouldBeGreaterThanOrEqualTo(0);
                vertices.IndexOf((5, Grammar.V(Var.L))).ShouldBeGreaterThanOrEqualTo(0);
                vertices.IndexOf((7, Grammar.V(Var.R))).ShouldBeGreaterThanOrEqualTo(0);
                vertices.IndexOf((7, Grammar.V(Var.L))).ShouldBeGreaterThanOrEqualTo(0);
            }
        }

        /// <summary>
        /// First step
        /// </summary>
        [Fact]
        public void ReadSets()
        {
            foreach (var (grammar, dfaLr0) in new[]
            {
                (Grammar, DfaLr0),
                (GrammarEof, DfaLr0Eof)
            })
            {
                var analyzer = Analyzers.CreateErasableSymbolsAnalyzer(grammar);

                var vertices = LalrLookaheadSetsAlgorithm.GetGotoTransitionPairs(grammar, dfaLr0);

                // Read(p,A) (aka INITFOLLOW(p,A)) sets
                var (directReads, graphRead) =
                    LalrLookaheadSetsAlgorithm.GetGraphReads(grammar, dfaLr0, vertices, analyzer);

                directReads[vertices.IndexOf((1, Grammar.V(Var.S)))].ShouldSetEqual(Grammar.T(Sym.EOF));
                directReads[vertices.IndexOf((1, Grammar.V(Var.R)))].ShouldBeEmpty();
                directReads[vertices.IndexOf((1, Grammar.V(Var.L)))].ShouldSetEqual(Grammar.T(Sym.EQUAL));
                directReads[vertices.IndexOf((5, Grammar.V(Var.R)))].ShouldBeEmpty();
                directReads[vertices.IndexOf((5, Grammar.V(Var.L)))].ShouldBeEmpty();
                directReads[vertices.IndexOf((7, Grammar.V(Var.R)))].ShouldBeEmpty();
                directReads[vertices.IndexOf((7, Grammar.V(Var.L)))].ShouldBeEmpty();

                // No epsilon-productions means the graph have no edges, and therefore are direct reads equal to reads
                Set<Terminal<Sym>>[] readSets = DigraphAlgorithm.Traverse(graphRead, directReads);

                readSets[vertices.IndexOf((1, Grammar.V(Var.S)))].ShouldSetEqual(Grammar.Eof());
                readSets[vertices.IndexOf((1, Grammar.V(Var.R)))].ShouldBeEmpty();
                readSets[vertices.IndexOf((1, Grammar.V(Var.L)))].ShouldSetEqual(Grammar.T(Sym.EQUAL));
                readSets[vertices.IndexOf((5, Grammar.V(Var.R)))].ShouldBeEmpty();
                readSets[vertices.IndexOf((5, Grammar.V(Var.L)))].ShouldBeEmpty();
                readSets[vertices.IndexOf((7, Grammar.V(Var.R)))].ShouldBeEmpty();
                readSets[vertices.IndexOf((7, Grammar.V(Var.L)))].ShouldBeEmpty();
            }
        }

        /// <summary>
        /// Second step
        /// </summary>
        [Fact]
        public void FollowSets()
        {
            foreach (var (grammar, dfaLr0) in new[]
            {
                (Grammar, DfaLr0),
                (GrammarEof, DfaLr0Eof)
            })
            {
                var analyzer = Analyzers.CreateErasableSymbolsAnalyzer(grammar);

                var vertices = LalrLookaheadSetsAlgorithm.GetGotoTransitionPairs(grammar, dfaLr0);

                var readSets = new Set<Terminal<Sym>>[vertices.Count];
                for (int i = 0; i < vertices.Count; i++)
                    readSets[i] = new Set<Terminal<Sym>>();

                readSets[vertices.IndexOf((1, Grammar.V(Var.S)))].Add(Grammar.Eof());
                readSets[vertices.IndexOf((1, Grammar.V(Var.L)))].Add(Grammar.T(Sym.EQUAL));

                // Follow(p,A) sets
                var graphLaFollow = LalrLookaheadSetsAlgorithm.GetGraphLaFollow(grammar, dfaLr0, vertices, analyzer);
                Set<Terminal<Sym>>[] followSets = DigraphAlgorithm.Traverse(graphLaFollow, readSets);

                // Follow(1, S) = {$} etc...
                followSets[vertices.IndexOf((1, Grammar.V(Var.S)))].ShouldSetEqual(Grammar.Eof());
                followSets[vertices.IndexOf((1, Grammar.V(Var.R)))].ShouldSetEqual(Grammar.Eof());
                followSets[vertices.IndexOf((1, Grammar.V(Var.L)))].ShouldSetEqual(Grammar.Eof(), Grammar.T(Sym.EQUAL));
                followSets[vertices.IndexOf((5, Grammar.V(Var.R)))].ShouldSetEqual(Grammar.Eof(), Grammar.T(Sym.EQUAL));
                followSets[vertices.IndexOf((5, Grammar.V(Var.L)))].ShouldSetEqual(Grammar.Eof(), Grammar.T(Sym.EQUAL));
                followSets[vertices.IndexOf((7, Grammar.V(Var.R)))].ShouldSetEqual(Grammar.Eof());
                followSets[vertices.IndexOf((7, Grammar.V(Var.L)))].ShouldSetEqual(Grammar.Eof());

                followSets.Length.ShouldBe(7);
            }
        }

        /// <summary>
        /// Third step
        /// </summary>
        [Fact]
        public void UnionSets()
        {
            foreach (var (grammar, dfaLr0) in new[]
            {
                (Grammar, DfaLr0),
                (GrammarEof, DfaLr0Eof)
            })
            {
                var vertices = LalrLookaheadSetsAlgorithm.GetGotoTransitionPairs(grammar, dfaLr0);

                var followSets = new Set<Terminal<Sym>>[vertices.Count];
                for (int i = 0; i < vertices.Count; i++)
                    followSets[i] = new Set<Terminal<Sym>>();

                // Follow(1, S) = {$} etc...
                followSets[vertices.IndexOf((1, Grammar.V(Var.S)))].Add(Grammar.Eof());
                followSets[vertices.IndexOf((1, Grammar.V(Var.R)))].Add(Grammar.Eof());
                followSets[vertices.IndexOf((1, Grammar.V(Var.L)))].AddRange(Grammar.Eof(), Grammar.T(Sym.EQUAL));
                followSets[vertices.IndexOf((5, Grammar.V(Var.R)))].AddRange(Grammar.Eof(), Grammar.T(Sym.EQUAL));
                followSets[vertices.IndexOf((5, Grammar.V(Var.L)))].AddRange(Grammar.Eof(), Grammar.T(Sym.EQUAL));
                followSets[vertices.IndexOf((7, Grammar.V(Var.R)))].Add(Grammar.Eof());
                followSets[vertices.IndexOf((7, Grammar.V(Var.L)))].Add(Grammar.Eof());

                // LA(q, A → ω) = ∪{ Follow(p,A) | (q, A → ω) lookback (p,A) }
                // Key = (stateIndex, productionIndex)
                Dictionary<(int stateIndex, int productionIndex), Set<Terminal<Sym>>> lookaheadSets =
                    LalrLookaheadSetsAlgorithm.GetLaUnion(grammar, dfaLr0, vertices, followSets);

                // LA(2, S' → S)
                lookaheadSets[(2, 0)].ShouldSetEqual(Grammar.Eof());
                // LA(3, R → L)
                lookaheadSets[(3, 5)].ShouldSetEqual(Grammar.Eof());
                // LA(3, S → R)
                lookaheadSets[(4, 2)].ShouldSetEqual(Grammar.Eof());
                // LA(6, L → a)
                lookaheadSets[(6, 4)].ShouldSetEqual(Grammar.T(Sym.EQUAL), Grammar.Eof());
                // LA(8, L → *R)
                lookaheadSets[(8, 3)].ShouldSetEqual(Grammar.T(Sym.EQUAL), Grammar.Eof());
                // LA(9, R → L)
                lookaheadSets[(9, 5)].ShouldSetEqual(Grammar.T(Sym.EQUAL), Grammar.Eof());
                // LA(10, S → L=R)
                lookaheadSets[(10, 1)].ShouldSetEqual(Grammar.Eof());

                lookaheadSets.Count.ShouldBe(7);
            }
        }
    }
}
