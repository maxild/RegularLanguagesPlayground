using AutomataLib;
using ContextFreeGrammar;
using Shouldly;
using Xunit;

namespace UnitTests
{
    public class GrammarTests
    {
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
