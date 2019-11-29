using System.Collections.Generic;
using AutomataLib;
using ContextFreeGrammar;
using ContextFreeGrammar.Analyzers;
using Shouldly;
using Xunit;

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
            Grammar = new GrammarBuilder()
                .SetAnalyzer(Analyzers.CreateDigraphAlgorithmAnalyzer)
                .SetNonterminalSymbols(Symbol.Vs("S'", "S", "R", "L"))
                .SetTerminalSymbols(Symbol.Ts('=', '*', 'a'))
                .SetStartSymbol(Symbol.V("S'"))
                .AndProductions(
                    Symbol.V("S'").Derives(Symbol.V("S")),
                    Symbol.V("S").Derives(Symbol.V("L"), Symbol.T('='), Symbol.V("R")),
                    Symbol.V("S").Derives(Symbol.V("R")),
                    Symbol.V("L").Derives(Symbol.T('*'), Symbol.V("R")),
                    Symbol.V("L").Derives(Symbol.T('a')),
                    Symbol.V("R").Derives(Symbol.V("L"))
                );

            DfaLr0 = Grammar.GetLr0AutomatonDfa();
        }

        private Grammar<Nonterminal, Terminal> Grammar { get; }

        private Dfa<ProductionItemSet<Nonterminal, Terminal>, Symbol> DfaLr0 { get; }

        [Fact]
        public void Vertices()
        {
            var vertices = LalrLookaheadSetsAlgorithm.GetGotoTransitionPairs(Grammar, DfaLr0);

            vertices.IndexOf((1, Symbol.V("S"))).ShouldBeGreaterThanOrEqualTo(0);
            vertices.IndexOf((1, Symbol.V("R"))).ShouldBeGreaterThanOrEqualTo(0);
            vertices.IndexOf((1, Symbol.V("L"))).ShouldBeGreaterThanOrEqualTo(0);
            vertices.IndexOf((5, Symbol.V("R"))).ShouldBeGreaterThanOrEqualTo(0);
            vertices.IndexOf((5, Symbol.V("L"))).ShouldBeGreaterThanOrEqualTo(0);
            vertices.IndexOf((7, Symbol.V("R"))).ShouldBeGreaterThanOrEqualTo(0);
            vertices.IndexOf((7, Symbol.V("L"))).ShouldBeGreaterThanOrEqualTo(0);
        }

        /// <summary>
        /// First step
        /// </summary>
        [Fact]
        public void ReadSets()
        {
            var analyzer = Analyzers.CreateErasableSymbolsAnalyzer(Grammar);

            var vertices = LalrLookaheadSetsAlgorithm.GetGotoTransitionPairs(Grammar, DfaLr0);

            // Read(p,A) (aka INITFOLLOW(p,A)) sets
            var (directReads, graphRead) =
                LalrLookaheadSetsAlgorithm.GetGraphReads(Grammar, DfaLr0, vertices, analyzer);

            directReads[vertices.IndexOf((1, Symbol.V("S")))].ShouldSetEqual(Symbol.EofMarker);
            directReads[vertices.IndexOf((1, Symbol.V("R")))].ShouldBeEmpty();
            directReads[vertices.IndexOf((1, Symbol.V("L")))].ShouldSetEqual(Symbol.T('='));
            directReads[vertices.IndexOf((5, Symbol.V("R")))].ShouldBeEmpty();
            directReads[vertices.IndexOf((5, Symbol.V("L")))].ShouldBeEmpty();
            directReads[vertices.IndexOf((7, Symbol.V("R")))].ShouldBeEmpty();
            directReads[vertices.IndexOf((7, Symbol.V("L")))].ShouldBeEmpty();

            // No epsilon-productions means the graph have no edges, and therefore are direct reads equal to reads
            Set<Terminal>[] readSets = DigraphAlgorithm.Traverse(graphRead, directReads);

            readSets[vertices.IndexOf((1, Symbol.V("S")))].ShouldSetEqual(Symbol.EofMarker);
            readSets[vertices.IndexOf((1, Symbol.V("R")))].ShouldBeEmpty();
            readSets[vertices.IndexOf((1, Symbol.V("L")))].ShouldSetEqual(Symbol.T('='));
            readSets[vertices.IndexOf((5, Symbol.V("R")))].ShouldBeEmpty();
            readSets[vertices.IndexOf((5, Symbol.V("L")))].ShouldBeEmpty();
            readSets[vertices.IndexOf((7, Symbol.V("R")))].ShouldBeEmpty();
            readSets[vertices.IndexOf((7, Symbol.V("L")))].ShouldBeEmpty();
        }

        /// <summary>
        /// Second step
        /// </summary>
        [Fact]
        public void FollowSets()
        {
            var analyzer = Analyzers.CreateErasableSymbolsAnalyzer(Grammar);

            var vertices = LalrLookaheadSetsAlgorithm.GetGotoTransitionPairs(Grammar, DfaLr0);

            var readSets = new Set<Terminal>[vertices.Count];
            for (int i = 0; i < vertices.Count; i++)
                readSets[i] = new Set<Terminal>();

            readSets[vertices.IndexOf((1, Symbol.V("S")))].Add(Symbol.EofMarker);
            readSets[vertices.IndexOf((1, Symbol.V("L")))].Add(Symbol.T('='));

            // Follow(p,A) sets
            var graphLaFollow = LalrLookaheadSetsAlgorithm.GetGraphLaFollow(Grammar, DfaLr0, vertices, analyzer);
            Set<Terminal>[] followSets = DigraphAlgorithm.Traverse(graphLaFollow, readSets);

            // Follow(1, S) = {$} etc...
            followSets[vertices.IndexOf((1, Symbol.V("S")))].ShouldSetEqual(Symbol.EofMarker);
            followSets[vertices.IndexOf((1, Symbol.V("R")))].ShouldSetEqual(Symbol.EofMarker);
            followSets[vertices.IndexOf((1, Symbol.V("L")))].ShouldSetEqual(Symbol.EofMarker, Symbol.T('='));
            followSets[vertices.IndexOf((5, Symbol.V("R")))].ShouldSetEqual(Symbol.EofMarker, Symbol.T('='));
            followSets[vertices.IndexOf((5, Symbol.V("L")))].ShouldSetEqual(Symbol.EofMarker, Symbol.T('='));
            followSets[vertices.IndexOf((7, Symbol.V("R")))].ShouldSetEqual(Symbol.EofMarker);
            followSets[vertices.IndexOf((7, Symbol.V("L")))].ShouldSetEqual(Symbol.EofMarker);

            followSets.Length.ShouldBe(7);
        }

        /// <summary>
        /// Third step
        /// </summary>
        [Fact]
        public void UnionSets()
        {
            var vertices = LalrLookaheadSetsAlgorithm.GetGotoTransitionPairs(Grammar, DfaLr0);

            var followSets = new Set<Terminal>[vertices.Count];
            for (int i = 0; i < vertices.Count; i++)
                followSets[i] = new Set<Terminal>();

            // Follow(1, S) = {$} etc...
            followSets[vertices.IndexOf((1, Symbol.V("S")))].Add(Symbol.EofMarker);
            followSets[vertices.IndexOf((1, Symbol.V("R")))].Add(Symbol.EofMarker);
            followSets[vertices.IndexOf((1, Symbol.V("L")))].AddRange(Symbol.EofMarker, Symbol.T('='));
            followSets[vertices.IndexOf((5, Symbol.V("R")))].AddRange(Symbol.EofMarker, Symbol.T('='));
            followSets[vertices.IndexOf((5, Symbol.V("L")))].AddRange(Symbol.EofMarker, Symbol.T('='));
            followSets[vertices.IndexOf((7, Symbol.V("R")))].Add(Symbol.EofMarker);
            followSets[vertices.IndexOf((7, Symbol.V("L")))].Add(Symbol.EofMarker);

            // LA(q, A → ω) = ∪{ Follow(p,A) | (q, A → ω) lookback (p,A) }
            // Key = (stateIndex, productionIndex)
            Dictionary<(int, int), Set<Terminal>> lookaheadSets =
                LalrLookaheadSetsAlgorithm.GetLaUnion(Grammar, DfaLr0, vertices, followSets);

            // LA(2, S' → S)
            lookaheadSets[(2, 0)].ShouldSetEqual(Symbol.EofMarker);
            // LA(3, R → L)
            lookaheadSets[(3,  5)].ShouldSetEqual(Symbol.EofMarker);
            // LA(3, S → R)
            lookaheadSets[(4, 2)].ShouldSetEqual(Symbol.EofMarker);
            // LA(6, L → a)
            lookaheadSets[(6, 4)].ShouldSetEqual(Symbol.T('='), Symbol.EofMarker);
            // LA(8, L → *R)
            lookaheadSets[(8, 3)].ShouldSetEqual(Symbol.T('='), Symbol.EofMarker);
            // LA(9, R → L)
            lookaheadSets[(9, 5)].ShouldSetEqual(Symbol.T('='), Symbol.EofMarker);
            // LA(10, S → L=R)
            lookaheadSets[(10, 1)].ShouldSetEqual(Symbol.EofMarker);

            lookaheadSets.Count.ShouldBe(7);
        }

        [Fact]
        public void LaSetsWithEofMarker()
        {
            // 0: S' → S$     (the digraph algorithms will augment the unit production with eof marker)
            // 1: S  → L = R
            // 2: S  → R
            // 3: R  → *R
            // 4: R  → a     ('id' in Gallier notes)
            // 5: R  → L
            var grammar = new GrammarBuilder()
                .SetAnalyzer(Analyzers.CreateDigraphAlgorithmAnalyzer)
                .SetNonterminalSymbols(Symbol.Vs("S'", "S", "R", "L"))
                .SetTerminalSymbols(Symbol.Ts('=', '*', 'a').WithEofMarker())
                .SetStartSymbol(Symbol.V("S'"))
                .AndProductions(
                    Symbol.V("S'").Derives(Symbol.V("S"), Symbol.EofMarker),
                    Symbol.V("S").Derives(Symbol.V("L"), Symbol.T('='), Symbol.V("R")),
                    Symbol.V("S").Derives(Symbol.V("R")),
                    Symbol.V("L").Derives(Symbol.T('*'), Symbol.V("R")),
                    Symbol.V("L").Derives(Symbol.T('a')),
                    Symbol.V("R").Derives(Symbol.V("L"))
                );

            var dfaLr0 = grammar.GetLr0AutomatonDfa();

            var analyzer = Analyzers.CreateErasableSymbolsAnalyzer(grammar);

            var sut = new Lr0AutomatonDigraphAnalyzer<Nonterminal, Terminal>(grammar, dfaLr0, analyzer);

            // BUG: eof marker changes state indices, because extra state 7 state is added
            // Solution: We must update GOTO-not-to-transition on $ in ToDfa method
            // LA(2, S' → S)
            //sut.Lookaheads(2, 0).ShouldSetEqual(Symbol.EofMarker);
            sut.Lookaheads(7, 0).ShouldSetEqual(Symbol.EofMarker);
            // LA(3, R → L)
            sut.Lookaheads(3, 5).ShouldSetEqual(Symbol.EofMarker);
            // LA(3, S → R)
            sut.Lookaheads(4, 2).ShouldSetEqual(Symbol.EofMarker);
            // LA(6, L → a)
            sut.Lookaheads(6, 4).ShouldSetEqual(Symbol.T('='), Symbol.EofMarker);
            // LA(8, L → *R)
            //sut.Lookaheads(8, 3).ShouldSetEqual(Symbol.T('='), Symbol.EofMarker);
            sut.Lookaheads(9, 3).ShouldSetEqual(Symbol.T('='), Symbol.EofMarker);
            // LA(9, R → L)
            //sut.Lookaheads(9, 5).ShouldSetEqual(Symbol.T('='), Symbol.EofMarker);
            sut.Lookaheads(10, 5).ShouldSetEqual(Symbol.T('='), Symbol.EofMarker);
            // LA(10, S → L=R)
            //sut.Lookaheads(10, 1).ShouldSetEqual(Symbol.EofMarker);
            sut.Lookaheads(11, 1).ShouldSetEqual(Symbol.EofMarker);

            sut.CountOfLookaheads.ShouldBe(7);
        }
    }
}
