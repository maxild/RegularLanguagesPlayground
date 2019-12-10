using AutomataLib;
using ContextFreeGrammar;
using GrammarRepo;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace UnitTests.TableTests
{
    public class StanfordShiftReduceConflictTableTests : XunitLoggingBase
    {
        public StanfordShiftReduceConflictTableTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void StanfordShiftReduceConflictGrammar()
        {
            var grammar = StanfordShiftReduceConflict.GetGrammar();

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
                writer.WriteLine($"In state {conflict.State}: {lr0Parser.GetItems(conflict.State).KernelItems.ToVectorString()} (kernel items)");
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

    }
}
