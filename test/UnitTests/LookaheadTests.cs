using AutomataLib;
using ContextFreeGrammar;
using ContextFreeGrammar.Analyzers;
using Shouldly;
using Xunit;

namespace UnitTests
{
    public class LookaheadTests
    {
        [Fact]
        public void GallierExample3()
        {
            // 0: S' → S$
            // 1: S  → L = R
            // 2: S  → R
            // 3: R  → *R
            // 4: R  → a    ('id' in Gallier notes)
            // 5: R  → L
            var grammar = new GrammarBuilder()
                .SetAnalyzer(Analyzers.CreateDigraphAlgorithmAnalyzer)
                .SetNonterminalSymbols(Symbol.Vs("S'", "S", "R", "L"))
                .SetTerminalSymbols(Symbol.Ts('=', '*', 'a').WithEofMarker()) // augmented grammar with terminals T U {$}
                .SetStartSymbol(Symbol.V("S'"))
                .AndProductions(
                    Symbol.V("S'").Derives(Symbol.V("S"), Symbol.EofMarker),
                    Symbol.V("S").Derives(Symbol.V("L"), Symbol.T('='), Symbol.V("R")),
                    Symbol.V("S").Derives(Symbol.V("R")),
                    Symbol.V("L").Derives(Symbol.T('*'), Symbol.V("R")),
                    Symbol.V("L").Derives(Symbol.T('a')),
                    Symbol.V("R").Derives(Symbol.V("L"))
                );

            // TODO: Implement the digraph algorithm for computing
            //          Read (INITFOLLOW) (traverse over Graph_Read using DR sets)
            //          Follow (traverse over Graph_LA using INITFOLLOW sets)
            //          LA sets (union over 'lookback' relation of CGA=LR(0)-automaton)
            grammar.ShouldNotBeNull();

        }
    }
}
