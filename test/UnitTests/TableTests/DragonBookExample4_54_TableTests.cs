using System.Diagnostics.CodeAnalysis;
using ContextFreeGrammar;
using ContextFreeGrammar.Lexers;
using GrammarRepo;
using Shouldly;
using Xunit;
using Xunit.Abstractions;
using Sym = GrammarRepo.DragonBookExample4_54.Sym;

namespace UnitTests.TableTests
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class DragonBookExample4_54_TableTests : XunitLoggingBase
    {
        public DragonBookExample4_54_TableTests(ITestOutputHelper output) : base(output)
        {
        }

        /// <summary>
        /// Dragon book example 4.54, p. 263, 2nd ed.
        /// </summary>
        [Fact]
        public void DragonBookEx4_54()
        {
            var grammar = DragonBookExample4_54.GetGrammar();

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

            // dccd
            var lexer = new FakeLexer<Sym>((Sym.d, "d"), (Sym.c, "c"), (Sym.c, "c"), (Sym.d, "d"));
            lr1Parser.Parse(lexer, writer);
        }
    }
}
