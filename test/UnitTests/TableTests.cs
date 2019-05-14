using System.Linq;
using AutomataLib;
using ContextFreeGrammar;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace UnitTests
{
    public class TableTests : XunitLoggingBase
    {
        public TableTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void CreateFollowResult()
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

            grammar.PrintFirstAndFollowSets(new TestWriter());
        }

        [Fact]
        public void CreateSlrTable()
        {
            // NOTE: Augmented, but without explicit EOF symbol (the EOF marker is optional, but changes the number of states)
            // 0: S → E
            // 1: E → E + T
            // 2: E → T
            // 3: T → T * F
            // 4: T → F
            // 5: T → (E)
            // 6: T → a        (Dragon book has 'id' terminal here, but our model only supports single char tokens at the moment)
            var grammar = new GrammarBuilder()
                .SetNonterminalSymbols(Symbol.Vs("S", "E", "T", "F"))
                .SetTerminalSymbols(Symbol.Ts('a', '+', '*', '(', ')'))
                .SetStartSymbol(Symbol.V("S"))
                .AndProductions(
                    Symbol.V("S").Derives(Symbol.V("E")), //, Symbol.EofMarker),
                    Symbol.V("E").Derives(Symbol.V("E"), Symbol.T('+'), Symbol.V("T")),
                    Symbol.V("E").Derives(Symbol.V("T")),
                    Symbol.V("T").Derives(Symbol.V("T"), Symbol.T('*'), Symbol.V("F")),
                    Symbol.V("T").Derives(Symbol.V("F")),
                    Symbol.V("F").Derives(Symbol.T('('), Symbol.V("E"), Symbol.T(')')),
                    Symbol.V("F").Derives(Symbol.T('a'))
                );

            // Grammar is not LR(0)
            grammar.ComputeLr0ParsingTable().AnyConflicts.ShouldBeTrue();

            var parser = grammar.ComputeSlrParsingTable();

            // Grammar is not SLR(1)
            parser.AnyConflicts.ShouldBeFalse();

            var writer = new TestWriter();

            parser.PrintParsingTable(writer);
            writer.WriteLine();
            writer.WriteLine();

            parser.Parse("a*a+a", writer);
        }

        [Fact]
        public void StanfordGrammar()
        {
            // 0: S → E
            // 1: E → E + T
            // 2: E → T
            // 3: T → (E)
            // 4: T → a
            var grammar = new GrammarBuilder()
                .SetNonterminalSymbols(Symbol.Vs("S", "E", "T"))
                .SetTerminalSymbols(Symbol.Ts('a', '+', '(', ')'))
                .SetStartSymbol(Symbol.V("S"))
                .AndProductions(
                    Symbol.V("S").Derives(Symbol.V("E")),
                    Symbol.V("E").Derives(Symbol.V("E"), Symbol.T('+'), Symbol.V("T")),
                    Symbol.V("E").Derives(Symbol.V("T")),
                    Symbol.V("T").Derives(Symbol.T('('), Symbol.V("E"), Symbol.T(')')),
                    Symbol.V("T").Derives(Symbol.T('a'))
                );

            var writer = new TestWriter();

            // Grammar is LR(0)
            var lr0Parser = grammar.ComputeLr0ParsingTable();
            lr0Parser.AnyConflicts.ShouldBeFalse();
            WriteLine("LR(0) table:");
            lr0Parser.PrintParsingTable(writer);

            // Grammar is SLR(1)
            var slrParser = grammar.ComputeSlrParsingTable();
            slrParser.AnyConflicts.ShouldBeFalse();
            WriteLine("SLR(1) table:");
            slrParser.PrintParsingTable(writer);
        }

        [Fact]
        public void StanfordShiftReduceConflictGrammar()
        {
            // 0: S → E
            // 1: E → E + T
            // 2: E → T
            // 3: T → (E)
            // 4: T → a          (Dragon book has 'id' terminal here, but our model only supports single char tokens at the moment)
            // 5: T → a[E]       (Dragon book has 'id' terminal here, but our model only supports single char tokens at the moment)
            var grammar = new GrammarBuilder()
                .SetNonterminalSymbols(Symbol.Vs("S", "E", "T"))
                .SetTerminalSymbols(Symbol.Ts('a', '+', '(', ')', '[', ']'))
                .SetStartSymbol(Symbol.V("S"))
                .AndProductions(
                    Symbol.V("S").Derives(Symbol.V("E")),
                    Symbol.V("E").Derives(Symbol.V("E"), Symbol.T('+'), Symbol.V("T")),
                    Symbol.V("E").Derives(Symbol.V("T")),
                    Symbol.V("T").Derives(Symbol.T('('), Symbol.V("E"), Symbol.T(')')),
                    Symbol.V("T").Derives(Symbol.T('a')),
                    // Adding this rule we have a shift/reduce conflict {shift 7, reduce 4} on '[' in state 4,
                    // because state 4 contains the following core items {T → a•, T → a•[E]}
                    Symbol.V("T").Derives(Symbol.T('a'), Symbol.T('['), Symbol.V("E"), Symbol.T(']'))
                );

            var writer = new TestWriter();

            writer.WriteLine(grammar.ToString());
            writer.WriteLine();

            grammar.PrintFirstAndFollowSets(new TestWriter());
            writer.WriteLine();
            writer.WriteLine();

            var lr0Parser = grammar.ComputeLr0ParsingTable();

            // Grammar is not LR(0)
            lr0Parser.AnyConflicts.ShouldBeTrue();

            foreach (var conflict in lr0Parser.Conflicts)
            {
                writer.WriteLine(conflict);
                writer.WriteLine($"In state {conflict.State}: {lr0Parser.GetItems(conflict.State).CoreItems.ToVectorString()} (core items)");
            }
            writer.WriteLine();
            writer.WriteLine();

            var slrParser = grammar.ComputeSlrParsingTable();

            // Grammar is SLR(1)
            slrParser.AnyConflicts.ShouldBeFalse();

            slrParser.PrintParsingTable(writer);
            writer.WriteLine();
            writer.WriteLine();


        }

        [Fact]
        public void StanfordReduceReduceConflictGrammar()
        {
            // 0: S → E
            // 1: E → E + T
            // 2: E → T
            // 3: E → V = E
            // 4: T → (E)
            // 5: T → a          (Dragon book has 'id' terminal here, but our model only supports single char tokens at the moment)
            // 6: V → a          (Dragon book has 'id' terminal here, but our model only supports single char tokens at the moment)
            var grammar = new GrammarBuilder()
                .SetNonterminalSymbols(Symbol.Vs("S", "E", "T", "V"))
                .SetTerminalSymbols(Symbol.Ts('a', '+', '(', ')', '='))
                .SetStartSymbol(Symbol.V("S"))
                .AndProductions(
                    Symbol.V("S").Derives(Symbol.V("E")),
                    Symbol.V("E").Derives(Symbol.V("E"), Symbol.T('+'), Symbol.V("T")),
                    Symbol.V("E").Derives(Symbol.V("T")),
                    // Adding this rule we have a reduce/reduce conflict {reduce 5, reduce 6} in state 5 on every
                    // possible symbol (in LR(0) table), because state 5 contains the following core items {T → a•, V → a•}
                    Symbol.V("E").Derives(Symbol.V("V"), Symbol.T('='), Symbol.V("E")),
                    Symbol.V("T").Derives(Symbol.T('('), Symbol.V("E"), Symbol.T(')')),
                    Symbol.V("T").Derives(Symbol.T('a')),
                    Symbol.V("V").Derives(Symbol.T('a'))
                );

            var writer = new TestWriter();

            writer.WriteLine(grammar.ToString());
            writer.WriteLine();

            grammar.PrintFirstAndFollowSets(new TestWriter());
            writer.WriteLine();
            writer.WriteLine();

            // Grammar is not LR(0)
            grammar.ComputeLr0ParsingTable().AnyConflicts.ShouldBeTrue();

            var slrParser = grammar.ComputeSlrParsingTable();

            // The grammar is SLR(1)
            slrParser.AnyConflicts.ShouldBeTrue();

            slrParser.PrintParsingTable(writer);
            writer.WriteLine();
            writer.WriteLine();

            foreach (var conflict in slrParser.Conflicts)
            {
                writer.WriteLine(conflict);
                writer.WriteLine($"In state {conflict.State}: {slrParser.GetItems(conflict.State).CoreItems.ToVectorString()} (core items)");
            }
            writer.WriteLine();
            writer.WriteLine();
        }

        // TODO: make grammar repository/factory

        [Fact]
        public void DragonBookEx4_48()
        {
            // NOTE: We are using nonterminal L for l-value (a location), nonterminal R for r-value (value
            //       that can be stored in a location), and terminal * for 'content-of' prefix operator.
            // 0: S' → S
            // 1: S → L = R
            // 2: S → R
            // 3: L → *R
            // 4: L → a        (Dragon book has 'id' terminal here, but our model only supports single char tokens at the moment)
            // 5: R → L
            var grammar = new GrammarBuilder()
                .SetNonterminalSymbols(Symbol.Vs("S'", "S", "L", "R"))
                .SetTerminalSymbols(Symbol.Ts('a', '=', '*'))
                .SetStartSymbol(Symbol.V("S'"))
                .AndProductions(
                    Symbol.V("S'").Derives(Symbol.V("S")),
                    Symbol.V("S").Derives(Symbol.V("L"), Symbol.T('='), Symbol.V("R")),
                    Symbol.V("S").Derives(Symbol.V("R")),
                    Symbol.V("L").Derives(Symbol.T('*'), Symbol.V("R")),
                    Symbol.V("L").Derives(Symbol.T('a')),
                    Symbol.V("R").Derives(Symbol.V("L"))
                );

            var writer = new TestWriter();

            grammar.PrintFirstAndFollowSets(writer);

            WriteLine("SLR(1) Parsing Table");
            var slrParser = grammar.ComputeSlrParsingTable();

            slrParser.PrintParsingTable(writer);

            // Grammar is not SLR(1)
            slrParser.AnyConflicts.ShouldBeTrue();
            slrParser.Conflicts.Count().ShouldBe(1);

            // This will print
            //      State 2: {shift 6, reduce 5} on '='
            //      State 2: {S → L•=R, R → L•} (core items)
            // The correct choice of the parser is to shift, because no right sentential form begins with....TODO
            // Therefore....TODO
            var conflict = slrParser.Conflicts.Single();
            writer.WriteLine(conflict);
            writer.WriteLine($"State {conflict.State}: {slrParser.GetItems(conflict.State).CoreItems.ToVectorString()} (core items)");
            //  ╔════════╤══════════════╤══════════════╤══════════╤══════════════════════════════════╗
            //  ║ SeqNo  │    Stack     │   Symbols    │  Input   │              Action              ║
            //  ╠════════╪══════════════╪══════════════╪══════════╪══════════════════════════════════╣
            //  ║  (1)   │ 0            │              │     a=a$ │ shift 5                          ║
            //  ║  (2)   │ 0 5          │ a            │      =a$ │ reduce by L → a, goto 2          ║
            //  ║  (3)   │ 0 2          │ L            │      =a$ │ shift 6                          ║<--- shift/reduce conflict here (shift wins)
            //  ║  (4)   │ 0 2 6        │ L =          │       a$ │ shift 5                          ║
            //  ║  (5)   │ 0 2 6 5      │ L = a        │        $ │ reduce by L → a, goto 8          ║
            //  ║  (6)   │ 0 2 6 8      │ L = L        │        $ │ reduce by R → L, goto 9          ║
            //  ║  (7)   │ 0 2 6 9      │ L = R        │        $ │ reduce by S → L=R, goto 1        ║
            //  ║  (8)   │ 0 1          │ S            │        $ │ accept                           ║
            //  ╚════════╧══════════════╧══════════════╧══════════╧══════════════════════════════════╝
            slrParser.Parse("a=a", writer);

            var lr1Parser = grammar.ComputeLr1ParsingTable();

            WriteLine("LR(1) Parsing Table");
            lr1Parser.PrintParsingTable(writer);

            // Grammar is LR(1)
            lr1Parser.AnyConflicts.ShouldBeFalse();

            //  ╔════════╤══════════════╤══════════════╤══════════╤══════════════════════════════════╗
            //  ║ SeqNo  │    Stack     │   Symbols    │  Input   │              Action              ║
            //  ╠════════╪══════════════╪══════════════╪══════════╪══════════════════════════════════╣
            //  ║  (1)   │ 0            │              │     a=a$ │ shift 5                          ║
            //  ║  (2)   │ 0 5          │ a            │      =a$ │ reduce by L → a, goto 2          ║
            //  ║  (3)   │ 0 2          │ L            │      =a$ │ shift 6                          ║<--- no reduce, '=' is not a valid lookahead
            //  ║  (4)   │ 0 2 6        │ L =          │       a$ │ shift 12                         ║
            //  ║  (5)   │ 0 2 6 12     │ L = a        │        $ │ reduce by L → a, goto 10         ║
            //  ║  (6)   │ 0 2 6 10     │ L = L        │        $ │ reduce by R → L, goto 9          ║
            //  ║  (7)   │ 0 2 6 9      │ L = R        │        $ │ reduce by S → L=R, goto 1        ║
            //  ║  (8)   │ 0 1          │ S            │        $ │ accept                           ║
            //  ╚════════╧══════════════╧══════════════╧══════════╧══════════════════════════════════╝
            lr1Parser.Parse("a=a", writer);
        }

        /// <summary>
        /// Dragon book example 4.52, p. 261, 2nd ed.
        /// (identical to example 4.54, p. 263)
        /// </summary>
        [Fact]
        public void DragonBookEx4_52()
        {
            // Regular language for a*ba*b
            // 0: S' → S
            // 1: S → BB
            // 2: B → aB
            // 3: B → b
            var grammar = new GrammarBuilder()
                .SetNonterminalSymbols(Symbol.Vs("S'", "S", "B"))
                .SetTerminalSymbols(Symbol.Ts('a', 'b'))
                .SetStartSymbol(Symbol.V("S'"))
                .AndProductions(
                    Symbol.V("S'").Derives(Symbol.V("S")),
                    Symbol.V("S").Derives(Symbol.V("B"), Symbol.V("B")),
                    Symbol.V("B").Derives(Symbol.T('a'), Symbol.V("B")),
                    Symbol.V("B").Derives(Symbol.T('b'))
                );

            var writer = new TestWriter();

            //
            // LR(0)
            //

            var lr0Parser = grammar.ComputeLr0ParsingTable();

            WriteLine("LR(0) Parsing Table");
            lr0Parser.PrintParsingTable(writer);

            // The grammar is LR(0)
            lr0Parser.AnyConflicts.ShouldBeFalse();

            //
            // SLR(1)
            //

            var slrParser = grammar.ComputeSlrParsingTable();

            WriteLine("SLR(1) Parsing Table");
            slrParser.PrintParsingTable(writer);

            // The grammar is SLR(1)
            slrParser.AnyConflicts.ShouldBeFalse();

            WriteLine("Moves of SLR(1) parser");
            slrParser.Parse("baab", writer);

            //
            // LR(1)
            //

            var lr1Parser = grammar.ComputeLr1ParsingTable();

            WriteLine("LR(1) Parsing Table");
            lr1Parser.PrintParsingTable(writer);

            // The grammar is LR(1)
            lr1Parser.AnyConflicts.ShouldBeFalse();

            WriteLine("Moves of LR(1) parser");
            lr1Parser.Parse("baab", writer);

            //
            // LALR(1)
            //

            // BUG: There are conflicts, but grammar is LALR(1)
            // State 3: {shift 3, shift 3} on 'a'
            // State 3: {B → a•B} (core items)
            // State 3: {shift 4, shift 4} on 'b'
            // State 3: {B → a•B} (core items)

            var lalr1Parser = grammar.ComputeLalr1ParsingTable();

            WriteLine("LALR(1) Parsing Table");
            lalr1Parser.PrintParsingTable(writer);

            // TODO: Create utility method PrintAnyConflicts....and use it everywhere
            foreach (var conflict in lalr1Parser.Conflicts)
            {
                writer.WriteLine(conflict);
                // TODO: Show lookahead sets of items
                writer.WriteLine($"State {conflict.State}: {slrParser.GetItems(conflict.State).CoreItems.ToVectorString()} (core items)");
            }

            // The grammar is LALR(1)
            lalr1Parser.AnyConflicts.ShouldBeFalse();

            WriteLine("Moves of LALR(1) parser");
            lalr1Parser.Parse("baab", writer);
        }

        /// <summary>
        /// Dragon book example 4.54, p. 263, 2nd ed.
        /// </summary>
        [Fact]
        public void DragonBookEx4_54()
        {
            // Regular Language for c*dc*d
            // 0: S' → S
            // 1: S → CC
            // 2: C → cC
            // 3: C → d
            var grammar = new GrammarBuilder()
                .SetNonterminalSymbols(Symbol.Vs("S'", "S", "C"))
                .SetTerminalSymbols(Symbol.Ts('c', 'd'))
                .SetStartSymbol(Symbol.V("S'"))
                .AndProductions(
                    Symbol.V("S'").Derives(Symbol.V("S")),
                    Symbol.V("S").Derives(Symbol.V("C"), Symbol.V("C")),
                    Symbol.V("C").Derives(Symbol.T('c'), Symbol.V("C")),
                    Symbol.V("C").Derives(Symbol.T('d'))
                );

            var writer = new TestWriter();

            var lr0Parser = grammar.ComputeLr0ParsingTable();

            WriteLine("LR(0) Parsing Table");
            lr0Parser.PrintParsingTable(writer);

            // The grammar is LR(0)
            lr0Parser.AnyConflicts.ShouldBeFalse();

            var slrParser = grammar.ComputeSlrParsingTable();

            WriteLine("SLR(1) Parsing Table");
            slrParser.PrintParsingTable(writer);

            // The grammar is SLR(1)
            slrParser.AnyConflicts.ShouldBeFalse();

            var lr1Parser = grammar.ComputeLr1ParsingTable();

            WriteLine("LR(1) Parsing Table");
            lr1Parser.PrintParsingTable(writer);

            // The grammar is LR(1)
            lr1Parser.AnyConflicts.ShouldBeFalse();

            lr1Parser.Parse("dccd", writer);
        }
    }
}
