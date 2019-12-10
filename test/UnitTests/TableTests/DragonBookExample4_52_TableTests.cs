using System.Diagnostics.CodeAnalysis;
using AutomataLib;
using ContextFreeGrammar;
using ContextFreeGrammar.Lexers;
using GrammarRepo;
using Shouldly;
using Xunit;
using Xunit.Abstractions;
using Sym = GrammarRepo.DragonBookExample4_52.Sym;

namespace UnitTests.TableTests
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class DragonBookExample4_52_TableTests : XunitLoggingBase
    {
        public DragonBookExample4_52_TableTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void DragonBookEx4_52()
        {
            var writer = new TestWriter();

            var grammar = DragonBookExample4_52.GetGrammar();

            //
            // LR(0)
            //

            var lr0Parser = grammar.ComputeLr0ParsingTable();

            WriteLine("LR(0) Parsing Table");
            lr0Parser.PrintParsingTable(writer);

            // The grammar is LR(0)
            lr0Parser.AnyConflicts.ShouldBeFalse();

            WriteLine("Moves of LR(0) parser");
            lr0Parser.Parse(DragonBookExample4_52.GetLexer("baab"), writer);

            //
            // SLR(1)
            //

            var slrParser = grammar.ComputeSlrParsingTable();

            WriteLine("SLR(1) Parsing Table");
            slrParser.PrintParsingTable(writer);

            // The grammar is SLR(1)
            slrParser.AnyConflicts.ShouldBeFalse();

            WriteLine("Moves of SLR(1) parser");
            slrParser.Parse(DragonBookExample4_52.GetLexer("baab"), writer);

            //
            // LR(1)
            //

            var lr1Parser = grammar.ComputeLr1ParsingTable();

            WriteLine("LR(1) Parsing Table");
            lr1Parser.PrintParsingTable(writer);

            // The grammar is LR(1)
            lr1Parser.AnyConflicts.ShouldBeFalse();

            WriteLine("Moves of LR(1) parser");
            lr1Parser.Parse(DragonBookExample4_52.GetLexer("baab"), writer);

            //
            // LALR(1)
            //

            var lalr1Parser = grammar.ComputeLalrParsingTable();

            WriteLine("LALR(1) Parsing Table");
            lalr1Parser.PrintParsingTable(writer);

            // TODO: Create utility method PrintAnyConflicts....and use it everywhere
            foreach (var conflict in lalr1Parser.Conflicts)
            {
                writer.WriteLine(conflict);
                // TODO: Show lookahead sets of items
                writer.WriteLine($"State {conflict.State}: {slrParser.GetItems(conflict.State).KernelItems.ToVectorString()} (kernel items)");
            }

            // The grammar is LALR(1)
            lalr1Parser.AnyConflicts.ShouldBeFalse();

            WriteLine("Moves of LALR(1) parser");
            lalr1Parser.Parse(DragonBookExample4_52.GetLexer("baab"), writer);
        }

    }
}
