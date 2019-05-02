using System.Linq;
using AutomataLib;
using ContextFreeGrammar;
using Shouldly;
using Xunit;

namespace UnitTests
{
    public class GrammarTests
    {
        [Fact]
        public void Nullable()
        {
            // 0: T → R
            // 1: T → aTc
            // 2: R → ε
            // 3: R → bR
            var grammar = new Grammar(Symbol.Vs("T", "R"), Symbol.Ts('a', 'b', 'c'), Symbol.V("T"))
            {
                Symbol.V("T").GoesTo(Symbol.V("R")),
                Symbol.V("T").GoesTo(Symbol.T('a'), Symbol.V("T"), Symbol.T('c')),
                Symbol.V("R").GoesTo(Symbol.Epsilon),
                Symbol.V("R").GoesTo(Symbol.T('b'), Symbol.V("R"))
            };

            grammar.NULLABLE(Symbol.V("T")).ShouldBeTrue();
            grammar.NULLABLE(Symbol.V("R")).ShouldBeTrue();

            grammar.NULLABLE(0).ShouldBeTrue();
            grammar.NULLABLE(1).ShouldBeFalse();
            grammar.NULLABLE(2).ShouldBeTrue();
            grammar.NULLABLE(3).ShouldBeFalse();
        }

        [Fact]
        public void First()
        {
            // 0: S → E$
            // 1: E → E+T
            // 2: E → T
            // 3: T -> T*F
            // 4: T -> F
            // 5: F -> (E)
            // 6: F -> -T
            // 7: F -> a
            var grammar = new Grammar(Symbol.Vs("S", "E", "T", "F"),
                                      Symbol.Ts('a', '+', '-', '*', '(', ')').WithEofMarker(),
                                      Symbol.V("S"))
            {
                Symbol.V("S").GoesTo(Symbol.V("E"), Symbol.Eof<Terminal>()),
                Symbol.V("E").GoesTo(Symbol.V("E"), Symbol.T('+'), Symbol.V("T")),
                Symbol.V("E").GoesTo(Symbol.V("T")),
                Symbol.V("T").GoesTo(Symbol.V("T"), Symbol.T('*'), Symbol.V("F")),
                Symbol.V("T").GoesTo(Symbol.V("F")),
                Symbol.V("F").GoesTo(Symbol.T('('), Symbol.V("E"), Symbol.T(')')),
                Symbol.V("F").GoesTo(Symbol.T('-'), Symbol.V("T")),
                Symbol.V("F").GoesTo(Symbol.T('a'))
            };

            // No ε-productions, no nullable symbols
            grammar.Variables.Each(symbol => grammar.NULLABLE(symbol).ShouldBeFalse());
            Enumerable.Range(0, grammar.Productions.Count).Each(i => grammar.NULLABLE(i).ShouldBeFalse());

            // TODO: Better assertion failure messages for set comparisons

            // FIRST(X) for all X in T (i.e. all nonterminal symbols, aka variables)
            grammar.FIRST(Symbol.V("E")).SetEquals(Symbol.Ts('(', '-', 'a')).ShouldBeTrue();
            grammar.FIRST(Symbol.V("T")).SetEquals(Symbol.Ts('(', '-', 'a')).ShouldBeTrue();
            grammar.FIRST(Symbol.V("F")).SetEquals(Symbol.Ts('(', '-', 'a')).ShouldBeTrue();

            // FIRST(Y1 Y2...Yn) for all X → Y1 Y2...Yn in P (i.e. all productions)
            grammar.FIRST(0).SetEquals(Symbol.Ts('(', '-', 'a')).ShouldBeTrue();
            grammar.FIRST(1).SetEquals(Symbol.Ts('(', '-', 'a')).ShouldBeTrue();
            grammar.FIRST(2).SetEquals(Symbol.Ts('(', '-', 'a')).ShouldBeTrue();
            grammar.FIRST(3).SetEquals(Symbol.Ts('(', '-', 'a')).ShouldBeTrue();
            grammar.FIRST(4).SetEquals(Symbol.Ts('(', '-', 'a')).ShouldBeTrue();
            grammar.FIRST(5).SetEquals(Symbol.Ts('(')).ShouldBeTrue();
            grammar.FIRST(6).SetEquals(Symbol.Ts('-')).ShouldBeTrue();
            grammar.FIRST(7).SetEquals(Symbol.Ts('a')).ShouldBeTrue();
        }

        [Fact]
        public void Follow()
        {
            // 0: S → E$
            // 1: E → E+T
            // 2: E → T
            // 3: T -> T*F
            // 4: T -> F
            // 5: F -> (E)
            // 6: F -> -T
            // 7: F -> a
            var grammar = new Grammar(Symbol.Vs("S", "E", "T", "F"),
                Symbol.Ts('a', '+', '-', '*', '(', ')').WithEofMarker(),
                Symbol.V("S"))
            {
                Symbol.V("S").GoesTo(Symbol.V("E"), Symbol.Eof<Terminal>()),
                Symbol.V("E").GoesTo(Symbol.V("E"), Symbol.T('+'), Symbol.V("T")),
                Symbol.V("E").GoesTo(Symbol.V("T")),
                Symbol.V("T").GoesTo(Symbol.V("T"), Symbol.T('*'), Symbol.V("F")),
                Symbol.V("T").GoesTo(Symbol.V("F")),
                Symbol.V("F").GoesTo(Symbol.T('('), Symbol.V("E"), Symbol.T(')')),
                Symbol.V("F").GoesTo(Symbol.T('-'), Symbol.V("T")),
                Symbol.V("F").GoesTo(Symbol.T('a'))
            };

            grammar.FOLLOW(Symbol.V("E")).SetEquals(Symbol.Ts('+', ')').WithEofMarker()).ShouldBeTrue();
            grammar.FOLLOW(Symbol.V("T")).SetEquals(Symbol.Ts('+', '*', ')').WithEofMarker()).ShouldBeTrue();
            grammar.FOLLOW(Symbol.V("F")).SetEquals(Symbol.Ts('+', '*', ')').WithEofMarker()).ShouldBeTrue();
        }

        [Fact]
        public void Stringify()
        {
            // Augmented Grammar (assumed reduced, i.e. no useless symbols).
            //
            // ({S,E}, {a,b}, P, S) with P given by
            //
            // The purpose of this new starting production (S) is to indicate to the parser when
            // it should stop parsing and announce acceptance of input.
            //
            // 0: S → E
            // 1: E → aEb
            // 2: E → ab
            var grammar = new Grammar(Symbol.Vs("S", "E"), Symbol.Ts('a', 'b'), Symbol.V("S"))
            {
                Symbol.V("S").GoesTo(Symbol.V("E")),
                Symbol.V("E").GoesTo(Symbol.T('a'), Symbol.V("E"), Symbol.T('b')),
                Symbol.V("E").GoesTo(Symbol.T('a'), Symbol.T('b'))
            };

            grammar.ToString().ShouldBe(@"0: S → E
1: E → aEb
2: E → ab
");

            // Create LR(0) Automaton
            grammar.IsReduced.ShouldBeTrue();
            grammar.IsAugmented.ShouldBeTrue();

            //var characteristicStringsNfa = grammar.GetCharacteristicStringsNfa();

            // create states: Create DFA (epsilon-closure)
            //var characteristicStringsDfa = ...

            // Create it directly...in single step
        }
    }
}
