using ContextFreeGrammar;
using GrammarRepo;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace UnitTests.TableTests
{
    public class StanfordExprGrammarTableTests : XunitLoggingBase
    {
        public StanfordExprGrammarTableTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void StanfordGrammar()
        {
            var grammar = StanfordExprGrammar.GetGrammar();

            var writer = new TestWriter();

            // Grammar is LR(0)
            var lr0Parser = grammar.ComputeLr0ParsingTable();
            lr0Parser.AnyConflicts.ShouldBeFalse();
            writer.WriteLine("LR(0) table:");
            lr0Parser.PrintParsingTable(writer);

            // Grammar is SLR(1)
            var slrParser = grammar.ComputeSlrParsingTable();
            slrParser.AnyConflicts.ShouldBeFalse();
            writer.WriteLine("SLR(1) table:");
            slrParser.PrintParsingTable(writer);
        }

    }
}
