using AutomataLib;
using ContextFreeGrammar;
using ContextFreeGrammar.Analyzers;
using GrammarRepo;
using Shouldly;
using Xunit;
using Sym = GrammarRepo.GallierCalc.Sym;

namespace UnitTests
{
    public class DigraphAlgorithmTests
    {
        public DigraphAlgorithmTests()
        {
            Grammar = GallierCalc.GetGrammar();
        }

        private Grammar<Sym> Grammar { get; }

        [Fact]
        public void GetFirstGraph()
        {
            var analyzer = Analyzers.CreateErasableSymbolsAnalyzer(Grammar);

            var (initSets, graph) = DigraphAlgorithm.GetFirstGraph(Grammar, analyzer);

            // INITFIRST(S)
            initSets[0].ShouldBeEmpty();
            // INITFIRST(E)
            initSets[1].ShouldBeEmpty();
            // INITFIRST(T)
            initSets[2].ShouldBeEmpty();
            // INITFIRST(F)
            initSets[3].ShouldSetEqual(Symbol.Ts(Sym.LPARAN, Sym.MINUS, Sym.ID));

            var closureSets = DigraphAlgorithm.Traverse(graph, new[]
            {
                // trick to inspect closure (unions)
                new Set<string> {"S"},
                new Set<string> {"E"},
                new Set<string> {"T"},
                new Set<string> {"F"}
            });

            // FIRST(S) = INITFIRST(S) ∪ INITFIRST(E) ∪ INITFIRST(T) ∪ INITFIRST(F)
            closureSets[0].ShouldSetEqual("S", "E", "T", "F");
            // FIRST(E) = INITFIRST(E) ∪ INITFIRST(T) ∪ INITFIRST(F)
            closureSets[1].ShouldSetEqual("E", "T", "F");
            // FIRST(T) = INITFIRST(T) ∪ INITFIRST(F)
            closureSets[2].ShouldSetEqual("T", "F");
            // FIRST(F) = INITFIRST(F)
            closureSets[3].ShouldSetEqual("F");
        }

        [Fact]
        public void GetFollowGraph()
        {
            var analyzer = Analyzers.CreateFirstSymbolsAnalyzer(Grammar);

            var (initSets, graph) = DigraphAlgorithm.GetFollowGraph(Grammar, analyzer);

            // INITFOLLOW(S)
            initSets[0].ShouldBeEmpty();
            // INITFOLLOW(E)
            initSets[1].ShouldSetEqual(Symbol.Ts(Sym.PLUS, Sym.RPARAN, Sym.EOF));
            // INITFOLLOW(T)
            initSets[2].ShouldSetEqual(Symbol.Ts(Sym.ASTERISK));
            // INITFOLLOW(F)
            initSets[3].ShouldBeEmpty();

            var closureSets = DigraphAlgorithm.Traverse(graph, new[]
            {
                // trick to inspect closure (unions)
                new Set<string> {"S"},
                new Set<string> {"E"},
                new Set<string> {"T"},
                new Set<string> {"F"}
            });

            // FOLLOW(S) = INITFOLLOW(S)
            closureSets[0].ShouldSetEqual("S");
            // FOLLOW(E) = INITFOLLOW(S) ∪ INITFOLLOW(E)
            closureSets[1].ShouldSetEqual("S", "E");
            // FOLLOW(T) = INITFOLLOW(S) ∪ INITFOLLOW(E) ∪ INITFOLLOW(T) ∪ INITFOLLOW(F)
            closureSets[2].ShouldSetEqual("S", "E", "T", "F");
            // FOLLOW(F) = INITFOLLOW(S) ∪ INITFOLLOW(E) ∪ INITFOLLOW(T) ∪ INITFOLLOW(F)
            closureSets[3].ShouldSetEqual("S", "E", "T", "F");
        }
    }
}
