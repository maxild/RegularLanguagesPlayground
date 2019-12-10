using AutomataLib;
using ContextFreeGrammar;
using GrammarRepo;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace UnitTests.TableTests
{
    public class StanfordReduceReduceConflictTableTests : XunitLoggingBase

    {
        public StanfordReduceReduceConflictTableTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void StanfordReduceReduceConflictGrammar()
        {
            var grammar = StanfordReduceReduceConflict.GetGrammar();

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
                writer.WriteLine(
                    $"In state {conflict.State}: {slrParser.GetItems(conflict.State).KernelItems.ToVectorString()} (kernel items)");
            }

            writer.WriteLine();
            writer.WriteLine();
        }

    }
}
