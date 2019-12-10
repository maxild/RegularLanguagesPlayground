using ContextFreeGrammar;
using ContextFreeGrammar.Lexers;
using GrammarRepo;
using Shouldly;
using Xunit;
using Xunit.Abstractions;
using Sym = GrammarRepo.DragonBook_ExprGrammarCh4.Sym;

namespace UnitTests.TableTests
{
    public class DragonBookExprGrammarCh4TableTests : XunitLoggingBase
    {
        public DragonBookExprGrammarCh4TableTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void CreateSlrTable()
        {
            var grammar = DragonBook_ExprGrammarCh4.GetGrammar();

            // Grammar is not LR(0)
            grammar.ComputeLr0ParsingTable().AnyConflicts.ShouldBeTrue();

            var parser = grammar.ComputeSlrParsingTable();

            // Grammar is not SLR(1)
            parser.AnyConflicts.ShouldBeFalse();

            var writer = new TestWriter();

            parser.PrintParsingTable(writer);
            writer.WriteLine();
            writer.WriteLine();

            // "a*a+a"
            var lexer = new FakeLexer<Sym>((Sym.ID, "a"), (Sym.ASTERISK, "*"), (Sym.ID, "a"), (Sym.PLUS, "+"),
                (Sym.ID, "a"));

            parser.Parse(lexer, writer);
        }
    }
}
