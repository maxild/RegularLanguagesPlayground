using AutomataLib;
using ContextFreeGrammar;
using ContextFreeGrammar.Analyzers;
using Shouldly;
using Xunit;

namespace UnitTests
{
    public class DigraphAlgorithmTests
    {
        public DigraphAlgorithmTests()
        {
            // Example 1 from the small survey of digraph methods https://www.cis.upenn.edu/~jean/gbooks/graphm.pdf
            // 0: S → E$
            // 1: E → E+T
            // 2: E → T
            // 3: T → T*F
            // 4: T → F
            // 5: F → (E)
            // 6: F → -T
            // 7: F → a
            Grammar = new GrammarBuilder()
                .SetNonterminalSymbols(Symbol.Vs("S", "E", "T", "F")) // insertion ordered
                .SetTerminalSymbols(Symbol.Ts('a', '+', '-', '*', '(', ')').WithEofMarker())
                .SetStartSymbol(Symbol.V("S"))
                .AndProductions(
                    Symbol.V("S").Derives(Symbol.V("E"), Symbol.EofMarker),
                    Symbol.V("E").Derives(Symbol.V("E"), Symbol.T('+'), Symbol.V("T")),
                    Symbol.V("E").Derives(Symbol.V("T")),
                    Symbol.V("T").Derives(Symbol.V("T"), Symbol.T('*'), Symbol.V("F")),
                    Symbol.V("T").Derives(Symbol.V("F")),
                    Symbol.V("F").Derives(Symbol.T('('), Symbol.V("E"), Symbol.T(')')),
                    Symbol.V("F").Derives(Symbol.T('-'), Symbol.V("T")),
                    Symbol.V("F").Derives(Symbol.T('a'))
                );
        }

        private Grammar<Nonterminal, Terminal> Grammar { get; }

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
            initSets[3].ShouldSetEqual(Symbol.Ts('(', '-', 'a'));

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
            initSets[1].ShouldSetEqual(Symbol.Ts('+', ')').WithEofMarker());
            // INITFOLLOW(T)
            initSets[2].ShouldSetEqual(Symbol.Ts('*'));
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
