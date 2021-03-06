using AutomataLib;
using ContextFreeGrammar;
using ContextFreeGrammar.Analyzers;
using GrammarRepo;
using Shouldly;
using UnitTests.Lexers;
using Xunit;

namespace UnitTests
{
    public class GrammarTests
    {
        //[Fact]
        //public void Nullable()
        //{
        //    // 0: T → R
        //    // 1: T → aTc
        //    // 2: R → ε
        //    // 3: R → bR
        //    var grammar = new GrammarBuilder()
        //        .SetAnalyzer(Analyzers.CreateDigraphAlgorithmAnalyzer)
        //        .SetNonterminalSymbols(Symbol.Vs("T", "R"))
        //        .SetTerminalSymbols(Symbol.Ts('a', 'b', 'c'))
        //        .SetStartSymbol(Symbol.V("T"))
        //        .AndProductions(
        //            Symbol.V("T").Derives(Symbol.V("R")),
        //            Symbol.V("T").Derives(Symbol.T('a'), Symbol.V("T"), Symbol.T('c')),
        //            Symbol.V("R").Derives(Symbol.Epsilon),
        //            Symbol.V("R").Derives(Symbol.T('b'), Symbol.V("R"))
        //        );

        //    grammar.Erasable(Symbol.V("T")).ShouldBeTrue();
        //    grammar.Erasable(Symbol.V("R")).ShouldBeTrue();

        //    grammar.Erasable(0).ShouldBeTrue();
        //    grammar.Erasable(1).ShouldBeFalse();
        //    grammar.Erasable(2).ShouldBeTrue();
        //    grammar.Erasable(3).ShouldBeFalse();
        //}

        [Fact]
        public void Stringify()
        {
            var grammar = GallierG1.GetGrammar();

            grammar.ToString().ShouldBe(@"0: S → E
1: E → a E b
2: E → a b
");

            grammar.IsReduced.ShouldBeTrue();
            grammar.IsAugmented.ShouldBeTrue();
        }

        //[Fact]
        //public void HuttonBookCh13()
        //{
        //    // NOTE: Both addition (+) and multiplication (*) are right-associative (unusual)!!
        //    // 0: S ::= <expr>
        //    // 1: <expr>   ::= <term> + <expr>
        //    // 2: <expr>   ::= <term>
        //    // 3: <term>   ::= <factor> * <term>
        //    // 4: <term>   ::= <factor>
        //    // 5: <factor> ::= ( <expr> )
        //    // 6: <factor> ::= nat
        //    // 7: <nat>    ::= 'a' (1 | ... | 9) (0 | 1 | ... | 9)* (this is just a token 'a')
        //    var grammar = new GrammarBuilder()
        //        .SetAnalyzer(Analyzers.CreateDigraphAlgorithmAnalyzer)
        //        //.SetNonterminalSymbols(Symbol.Vs("S", "expr", "term", "factor", "nat"))
        //        .SetNonterminalSymbols(Symbol.Vs("expr", "term", "factor", "nat"))
        //        .SetTerminalSymbols(Symbol.Ts('+', '*', '(', ')', 'a'))
        //        //.SetStartSymbol(Symbol.V("S"))
        //        .SetStartSymbol(Symbol.V("expr"))
        //        .AndProductions(
        //            //Symbol.V("S").Derives(Symbol.V("expr")),
        //            Symbol.V("expr").Derives(Symbol.V("term"), Symbol.T('+'), Symbol.V("expr")),
        //            Symbol.V("expr").Derives(Symbol.V("term")),
        //            Symbol.V("term").Derives(Symbol.V("factor"), Symbol.T('*'), Symbol.V("term")),
        //            Symbol.V("term").Derives(Symbol.V("factor")),
        //            Symbol.V("factor").Derives(Symbol.T('('), Symbol.V("expr"), Symbol.T(')')),
        //            Symbol.V("factor").Derives(Symbol.T('a'))
        //        );

        //    // The grammar is not LL(1) => Recursive Descent Parser is not an option

        //    //grammar.Erasable(Symbol.V("S")).ShouldBeFalse();
        //    grammar.Erasable(Symbol.V("expr")).ShouldBeFalse();
        //    grammar.Erasable(Symbol.V("term")).ShouldBeFalse();
        //    grammar.Erasable(Symbol.V("factor")).ShouldBeFalse();

        //    // First sets are not disjoint sets. They are all equal
        //    //grammar.First(Symbol.V("S")).ShouldSetEqual(Symbol.Ts('(', 'a'));
        //    grammar.First(Symbol.V("expr")).ShouldSetEqual(Symbol.Ts('(', 'a'));
        //    grammar.First(Symbol.V("term")).ShouldSetEqual(Symbol.Ts('(', 'a'));
        //    grammar.First(Symbol.V("factor")).ShouldSetEqual(Symbol.Ts('(', 'a'));

        //    // dragon book algorithm
        //    //grammar.Follow(Symbol.V("S")).ShouldSetEqual(Symbol.Ts().WithEofMarker());
        //    //grammar.Follow(Symbol.V("expr")).ShouldSetEqual(Symbol.Ts(')').WithEofMarker());
        //    //grammar.Follow(Symbol.V("term")).ShouldSetEqual(Symbol.Ts('+', ')').WithEofMarker());
        //    //grammar.Follow(Symbol.V("factor")).ShouldSetEqual(Symbol.Ts('*', '+', ')').WithEofMarker());

        //    // digraph algorithm (no eof marker in follow sets)
        //    //grammar.Follow(Symbol.V("S")).ShouldSetEqual(Symbol.Ts());
        //    grammar.Follow(Symbol.V("expr")).ShouldSetEqual(Symbol.Ts(')'));
        //    grammar.Follow(Symbol.V("term")).ShouldSetEqual(Symbol.Ts('+', ')'));
        //    grammar.Follow(Symbol.V("factor")).ShouldSetEqual(Symbol.Ts('*', '+', ')'));
        //}
    }
}
