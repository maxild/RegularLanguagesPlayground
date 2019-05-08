using AutomataLib;
using AutomataLib.Tables;
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
            var grammar = new Grammar(Symbol.Vs("S", "E", "T", "F"),
                Symbol.Ts('a', '+', '-', '*', '(', ')').WithEofMarker(),
                Symbol.V("S"))
            {
                Symbol.V("S").GoesTo(Symbol.V("E"), Symbol.EofMarker),
                Symbol.V("E").GoesTo(Symbol.V("E"), Symbol.T('+'), Symbol.V("T")),
                Symbol.V("E").GoesTo(Symbol.V("T")),
                Symbol.V("T").GoesTo(Symbol.V("T"), Symbol.T('*'), Symbol.V("F")),
                Symbol.V("T").GoesTo(Symbol.V("F")),
                Symbol.V("F").GoesTo(Symbol.T('('), Symbol.V("E"), Symbol.T(')')),
                Symbol.V("F").GoesTo(Symbol.T('-'), Symbol.V("T")),
                Symbol.V("F").GoesTo(Symbol.T('a'))
            };

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
            var grammar = new Grammar(
                variables: Symbol.Vs("S", "E", "T", "F"),
                terminals: Symbol.Ts('a', '+', '*', '(', ')'), //.WithEofMarker(),
                startSymbol: Symbol.V("S"))
            {
                Symbol.V("S").GoesTo(Symbol.V("E")), //, Symbol.EofMarker),
                Symbol.V("E").GoesTo(Symbol.V("E"), Symbol.T('+'), Symbol.V("T")),
                Symbol.V("E").GoesTo(Symbol.V("T")),
                Symbol.V("T").GoesTo(Symbol.V("T"), Symbol.T('*'), Symbol.V("F")),
                Symbol.V("T").GoesTo(Symbol.V("F")),
                Symbol.V("F").GoesTo(Symbol.T('('), Symbol.V("E"), Symbol.T(')')),
                Symbol.V("F").GoesTo(Symbol.T('a'))
            };

            var writer = new TestWriter();

            grammar.ComputeLr0ParsingTable().PrintParsingTable(writer);
            writer.WriteLine();
            writer.WriteLine();

            // TODO: Only works for SLR(1)...not for LR(0) table
            grammar.ComputeSlrParsingTable().Parse("a*a+a", writer);
        }

        [Fact]
        public void StanfordGrammar()
        {
            // 0: S → E
            // 1: E → E + T
            // 2: E → T
            // 3: T → (E)
            // 4: T → a
            var grammar = new Grammar(Symbol.Vs("S", "E", "T"), Symbol.Ts('a', '+', '(', ')'), Symbol.V("S"))
            {
                Symbol.V("S").GoesTo(Symbol.V("E")),
                Symbol.V("E").GoesTo(Symbol.V("E"), Symbol.T('+'), Symbol.V("T")),
                Symbol.V("E").GoesTo(Symbol.V("T")),
                Symbol.V("T").GoesTo(Symbol.T('('), Symbol.V("E"), Symbol.T(')')),
                Symbol.V("T").GoesTo(Symbol.T('a'))
            };

            var parser = grammar.ComputeSlrParsingTable();

            // TODO: Grammar is SLR(1), but not LR(0)
            parser.AnyConflicts.ShouldBeFalse();

            parser.PrintParsingTable(new TestWriter());
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
            var grammar = new Grammar(Symbol.Vs("S", "E", "T"), Symbol.Ts('a', '+', '(', ')', '[', ']'), Symbol.V("S"))
            {
                Symbol.V("S").GoesTo(Symbol.V("E")),
                Symbol.V("E").GoesTo(Symbol.V("E"), Symbol.T('+'), Symbol.V("T")),
                Symbol.V("E").GoesTo(Symbol.V("T")),
                Symbol.V("T").GoesTo(Symbol.T('('), Symbol.V("E"), Symbol.T(')')),
                Symbol.V("T").GoesTo(Symbol.T('a')),
                // Adding this rule we have a shift/reduce conflict {shift 7, reduce 4} on '[' in state 4,
                // because state 4 contains the following core items {T → a•, T → a•[E]}
                Symbol.V("T").GoesTo(Symbol.T('a'), Symbol.T('['), Symbol.V("E"), Symbol.T(']'))
                //

            };

            var writer = new TestWriter();

            writer.WriteLine(grammar.ToString());
            writer.WriteLine();

            grammar.PrintFirstAndFollowSets(new TestWriter());
            writer.WriteLine();
            writer.WriteLine();

            var parser = grammar.ComputeSlrParsingTable();

            // TODO: The grammar is SLR(1), not LR(0)...we should compute parse tables for both in a DRY fashion
            //parser.AnyConflicts.ShouldBeTrue();

            parser.PrintParsingTable(writer);
            writer.WriteLine();
            writer.WriteLine();

            foreach (var conflict in parser.Conflicts)
            {
                writer.WriteLine(conflict);
                writer.WriteLine($"In state {conflict.State}: {parser.GetItems(conflict.State).CoreItems.ToVectorString()} (core items)");
            }

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
            var grammar = new Grammar(Symbol.Vs("S", "E", "T", "V"), Symbol.Ts('a', '+', '(', ')', '='), Symbol.V("S"))
            {
                Symbol.V("S").GoesTo(Symbol.V("E")),
                Symbol.V("E").GoesTo(Symbol.V("E"), Symbol.T('+'), Symbol.V("T")),
                Symbol.V("E").GoesTo(Symbol.V("T")),
                // Adding this rule we have a reduce/reduce conflict {reduce 5, reduce 6} in state 5 on every
                // possible symbol (in LR(0) table), because state 5 contains the following core items {T → a•, V → a•}
                Symbol.V("E").GoesTo(Symbol.V("V"), Symbol.T('='), Symbol.V("E")),
                Symbol.V("T").GoesTo(Symbol.T('('), Symbol.V("E"), Symbol.T(')')),
                Symbol.V("T").GoesTo(Symbol.T('a')),
                Symbol.V("V").GoesTo(Symbol.T('a'))
            };

            var writer = new TestWriter();

            writer.WriteLine(grammar.ToString());
            writer.WriteLine();

            grammar.PrintFirstAndFollowSets(new TestWriter());
            writer.WriteLine();
            writer.WriteLine();

            var parser = grammar.ComputeSlrParsingTable();

            // TODO: The grammar is SLR(1), not LR(0)...we should compute parse tables for both in a DRY fashion
            parser.AnyConflicts.ShouldBeTrue();

            parser.PrintParsingTable(writer);
            writer.WriteLine();
            writer.WriteLine();

            foreach (var conflict in parser.Conflicts)
            {
                writer.WriteLine(conflict);
                writer.WriteLine($"In state {conflict.State}: {parser.GetItems(conflict.State).CoreItems.ToVectorString()} (core items)");
            }

        }
    }
}
