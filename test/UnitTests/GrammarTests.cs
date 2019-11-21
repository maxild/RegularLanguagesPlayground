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
            var grammar = new GrammarBuilder()
                .SetNonterminalSymbols(Symbol.Vs("T", "R"))
                .SetTerminalSymbols(Symbol.Ts('a', 'b', 'c'))
                .SetStartSymbol(Symbol.V("T"))
                .AndProductions(
                    Symbol.V("T").Derives(Symbol.V("R")),
                    Symbol.V("T").Derives(Symbol.T('a'), Symbol.V("T"), Symbol.T('c')),
                    Symbol.V("R").Derives(Symbol.Epsilon),
                    Symbol.V("R").Derives(Symbol.T('b'), Symbol.V("R"))
                );

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
            // 3: T → T*F
            // 4: T → F
            // 5: F → (E)
            // 6: F → -T
            // 7: F → a

            var grammar = new GrammarBuilder()
                .SetNonterminalSymbols(Symbol.Vs("S", "E", "T", "F"))
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

            // No ε-productions, no nullable symbols
            grammar.Variables.Each(symbol => grammar.NULLABLE(symbol).ShouldBeFalse());
            Enumerable.Range(0, grammar.Productions.Count).Each(i => grammar.NULLABLE(i).ShouldBeFalse());

            // FIRST(X) for all X in T (i.e. all nonterminal symbols, aka variables)
            grammar.FIRST(Symbol.V("S")).ShouldSetEqual(Symbol.Ts('(', '-', 'a'));
            grammar.FIRST(Symbol.V("E")).ShouldSetEqual(Symbol.Ts('(', '-', 'a'));
            grammar.FIRST(Symbol.V("T")).ShouldSetEqual(Symbol.Ts('(', '-', 'a'));
            grammar.FIRST(Symbol.V("F")).ShouldSetEqual(Symbol.Ts('(', '-', 'a'));

            // FIRST(Y1 Y2...Yn) for all X → Y1 Y2...Yn in P (i.e. all productions)
            grammar.FIRST(0).ShouldSetEqual(Symbol.Ts('(', '-', 'a'));
            grammar.FIRST(1).ShouldSetEqual(Symbol.Ts('(', '-', 'a'));
            grammar.FIRST(2).ShouldSetEqual(Symbol.Ts('(', '-', 'a'));
            grammar.FIRST(3).ShouldSetEqual(Symbol.Ts('(', '-', 'a'));
            grammar.FIRST(4).ShouldSetEqual(Symbol.Ts('(', '-', 'a'));
            grammar.FIRST(5).ShouldSetEqual(Symbol.Ts('('));
            grammar.FIRST(6).ShouldSetEqual(Symbol.Ts('-'));
            grammar.FIRST(7).ShouldSetEqual(Symbol.Ts('a'));
        }

        [Fact]
        public void Follow()
        {
            // 0: S → E$
            // 1: E → E+T
            // 2: E → T
            // 3: T → T*F
            // 4: T → F
            // 5: F → (E)
            // 6: F → -T
            // 7: F → a
            var grammar = new GrammarBuilder()
                .SetNonterminalSymbols(Symbol.Vs("S", "E", "T", "F"))
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

            grammar.FOLLOW(Symbol.V("E")).ShouldSetEqual(Symbol.Ts('+', ')').WithEofMarker());
            grammar.FOLLOW(Symbol.V("T")).ShouldSetEqual(Symbol.Ts('+', '*', ')').WithEofMarker());
            grammar.FOLLOW(Symbol.V("F")).ShouldSetEqual(Symbol.Ts('+', '*', ')').WithEofMarker());
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
            var grammar = new GrammarBuilder()
                .SetNonterminalSymbols(Symbol.Vs("S", "E"))
                .SetTerminalSymbols(Symbol.Ts('a', 'b'))
                .SetStartSymbol(Symbol.V("S"))
                .AndProductions(
                    Symbol.V("S").Derives(Symbol.V("E")),
                    Symbol.V("E").Derives(Symbol.T('a'), Symbol.V("E"), Symbol.T('b')),
                    Symbol.V("E").Derives(Symbol.T('a'), Symbol.T('b'))
                );

            grammar.ToString().ShouldBe(@"0: S → E
1: E → aEb
2: E → ab
");

            // Create LR(0) Automaton
            grammar.IsReduced.ShouldBeTrue();
            grammar.IsAugmented.ShouldBeTrue();


            //var characteristicStringsNfa = grammar.GetLr0AutomatonNfa();

            // create states: Create DFA (epsilon-closure)
            //var characteristicStringsDfa = ...

            // Create it directly...in single step
        }

        [Fact]
        public void HuttonBookCh13()
        {
            // NOTE: Both addition (+) and multiplication (*) are right-associative (unusual)!!
            // 0: S ::= <expr>
            // 1: <expr>   ::= <term> + <expr>
            // 2: <expr>   ::= <term>
            // 3: <term>   ::= <factor> * <term>
            // 4: <term>   ::= <factor>
            // 5: <factor> ::= ( <expr> )
            // 6: <factor> ::= nat
            // 7: <nat>    ::= 'a' (1 | ... | 9) (0 | 1 | ... | 9)* (this is just a token 'a')
            var grammar = new GrammarBuilder()
                .SetNonterminalSymbols(Symbol.Vs("S", "expr", "term", "factor", "nat"))
                .SetTerminalSymbols(Symbol.Ts('+', '*', '(', ')', 'a'))
                .SetStartSymbol(Symbol.V("S"))
                .AndProductions(
                    Symbol.V("S").Derives(Symbol.V("expr")),
                    Symbol.V("expr").Derives(Symbol.V("term"), Symbol.T('+'), Symbol.V("expr")),
                    Symbol.V("expr").Derives(Symbol.V("term")),
                    Symbol.V("term").Derives(Symbol.V("factor"), Symbol.T('*'), Symbol.V("term")),
                    Symbol.V("term").Derives(Symbol.V("factor")),
                    Symbol.V("factor").Derives(Symbol.T('('), Symbol.V("expr"), Symbol.T(')')),
                    Symbol.V("factor").Derives(Symbol.T('a'))
                );

            // The grammar is not LL(1) => Recursive Descent Parser is not an option

            grammar.NULLABLE(Symbol.V("S")).ShouldBeFalse();
            grammar.NULLABLE(Symbol.V("expr")).ShouldBeFalse();
            grammar.NULLABLE(Symbol.V("term")).ShouldBeFalse();
            grammar.NULLABLE(Symbol.V("factor")).ShouldBeFalse();

            // First sets are not disjoint sets. They are all equal
            grammar.FIRST(Symbol.V("S")).ShouldSetEqual(Symbol.Ts('(', 'a'));
            grammar.FIRST(Symbol.V("expr")).ShouldSetEqual(Symbol.Ts('(', 'a'));
            grammar.FIRST(Symbol.V("term")).ShouldSetEqual(Symbol.Ts('(', 'a'));
            grammar.FIRST(Symbol.V("factor")).ShouldSetEqual(Symbol.Ts('(', 'a'));

            grammar.FOLLOW(Symbol.V("S")).ShouldSetEqual(Symbol.Ts().WithEofMarker());
            grammar.FOLLOW(Symbol.V("expr")).ShouldSetEqual(Symbol.Ts(')').WithEofMarker());
            grammar.FOLLOW(Symbol.V("term")).ShouldSetEqual(Symbol.Ts('+', ')').WithEofMarker());
            grammar.FOLLOW(Symbol.V("factor")).ShouldSetEqual(Symbol.Ts('*', '+', ')').WithEofMarker());
        }
    }
}
