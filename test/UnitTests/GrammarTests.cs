using System;
using System.Collections.Generic;
using System.Linq;
using ContextFreeGrammar;
using Shouldly;
using Xunit;

namespace UnitTests
{
    public class GrammarTests
    {
        [Fact]
        public void Test()
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

            // Create NFA (digraph of items labeled by symbols)
            var characteristicStringsNfa = new Nfa<ProductionItem, Symbol>(new ProductionItem(grammar.Productions[0], 0, 0));

            // (a) For every terminal a in T, if A → α.aβ is a marked production, then
            //     there is a transition on input a from state A → α.aβ to state A → αa.β
            //     obtained by "shifting the dot"
            // (b) For every variable B in V, if A → α.Bβ is a marked production, then
            //     there is a transition on input B from state A → α.Bβ to state A → αB.β
            //     obtained by "shifting the dot", and transitions on input ϵ (the empty string)
            //     to all states B → .γ(i), for all productions B → γ(i) in P with left-hand side B.
            int productionIndex = 0;
            foreach (var production in grammar)
            {
                for (int dotPosition = 0; dotPosition <= production.Tail.Count; dotPosition += 1)
                {
                    // (productionIndex, dotPosition) is identifier
                    var item = new ProductionItem(production, productionIndex, dotPosition);

                    // (a) A → α.aβ
                    if (item.IsShiftItem)
                    {
                        // shift item
                        characteristicStringsNfa.AddTransition(item, item.GetNextSymbol<Terminal>(), item.GetNextItem());
                    }

                    // (b) A → α.Bβ
                    if (item.IsGotoItem)
                    {
                        var nonTerminal = item.GetNextSymbol<NonTerminal>();
                        // goto item
                        characteristicStringsNfa.AddTransition(item, nonTerminal, item.GetNextItem());
                        // closure items
                        foreach (var closureItems in grammar.GetEquivalentItemsOf(nonTerminal))
                        {
                            // Expecting to see a non terminal 'B' is the same as expecting to see
                            // RHS grammar symbols 'γ(i)', where B → γ(i) is a production in P
                            characteristicStringsNfa.AddTransition(item, Symbol.Epsilon, closureItems);
                        }
                    }

                    // (c) A → β. Accepting states has dot shifted all the way to the end.
                    if (item.IsReduceItem)
                    {
                        characteristicStringsNfa.AcceptingStates.Add(item);
                    }
                }

                productionIndex += 1;
            }

            // create states: Create DFA (epsilon-closure)
            //var characteristicStringsDfa = ...

            // Create it directly...in single step
        }
    }

    public class ProductionItemTests
    {
        [Fact]
        public void Test()
        {
            var production = Symbol.V("E").GoesTo(Symbol.V("E"), Symbol.T('+'), Symbol.V("E"));
            new ProductionItem(production, 0, 0).ToString().ShouldBe("E → •E+E");
            new ProductionItem(production, 0, 1).ToString().ShouldBe("E → E•+E");
            new ProductionItem(production, 0, 2).ToString().ShouldBe("E → E+•E");
            new ProductionItem(production, 0, 3).ToString().ShouldBe("E → E+E•");
            Assert.Throws<ArgumentException>(() => new ProductionItem(production, 0, 4));
        }
    }

    public static class GrammarExtensions
    {
        public static Production GoesTo(this NonTerminal head, params Symbol[] tail)
        {
            return new Production(head, tail);
        }
    }
}
