using ContextFreeGrammar;
using GrammarRepo;
using Xunit;
using Xunit.Abstractions;

namespace UnitTests.TableTests
{
    public class GallierCalcTableTests : XunitLoggingBase
    {
        public GallierCalcTableTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void CreateFollowResult()
        {
            var grammar = GallierCalc.GetGrammar();

            grammar.PrintFirstAndFollowSets(new TestWriter());
        }
    }
}
