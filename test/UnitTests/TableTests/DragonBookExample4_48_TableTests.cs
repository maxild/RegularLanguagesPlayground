using System.Diagnostics.CodeAnalysis;
using System.Linq;
using AutomataLib;
using ContextFreeGrammar;
using ContextFreeGrammar.Lexers;
using GrammarRepo;
using Shouldly;
using Xunit;
using Xunit.Abstractions;
using Sym = GrammarRepo.DragonBookExample4_48.Sym;
using Var = GrammarRepo.DragonBookExample4_48.Var;

namespace UnitTests.TableTests
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class DragonBookExample4_48_TableTests : XunitLoggingBase
    {
        public DragonBookExample4_48_TableTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void DragonBookEx4_48()
        {
            // NOTE: We are using nonterminal L for l-value (a location), nonterminal R for r-value (value
            //       that can be stored in a location), and terminal * for 'content-of' prefix operator.
            // 0: S' → S
            // 1: S → L = R
            // 2: S → R
            // 3: L → *R
            // 4: L → ID
            // 5: R → L
            Grammar<Sym, Var> grammar = DragonBookExample4_48.GetGrammar();

            var writer = new TestWriter();


            var lr0Dfa = grammar.GetLr0AutomatonDfa();
            lr0Dfa.PrintKernelItems(writer);

            grammar.PrintFirstAndFollowSets(writer);
            // ╔═══════════════════════════════════════════════════════════╗
            // ║                   First and Follow sets                   ║
            // ╠════════════╤════════════╤════════════════╤════════════════╣
            // ║  Variable  │  Nullable  │     First      │     Follow     ║
            // ╠════════════╪════════════╪════════════════╪════════════════╣
            // ║     S'     │   false    │ {ASTERISK, ID} │       {}       ║
            // ║     S      │   false    │ {ASTERISK, ID} │     {EOF}      ║
            // ║     R      │   false    │ {ASTERISK, ID} │  {EOF, EQUAL}  ║
            // ║     L      │   false    │ {ASTERISK, ID} │  {EQUAL, EOF}  ║
            // ╚════════════╧════════════╧════════════════╧════════════════╝

            grammar.Follow(grammar.V(Var.Start)).ShouldBeEmpty();
            grammar.Follow(grammar.V(Var.S)).ShouldSetEqual(grammar.Eof());
            grammar.Follow(grammar.V(Var.R)).ShouldSetEqual(grammar.T(Sym.EQUAL), grammar.Eof());
            grammar.Follow(grammar.V(Var.L)).ShouldSetEqual(grammar.T(Sym.EQUAL), grammar.Eof());

            WriteLine("SLR(1) Parsing Table");
            var slrParser = grammar.ComputeSlrParsingTable();

            // SLR(1) Parsing Table
            // ╔════════════════════════════════╗╔══════════════════════════╗
            // ║             ACTION             ║║           GOTO           ║
            // ╠════════╤═════╤═════╤═════╤═════╣╠════════╤═════╤═════╤═════╣
            // ║ State  │EQUAL│ ID  │ASTER│ EOF ║║ State  │  S  │  R  │  L  ║
            // ╠════════╪═════╪═════╪═════╪═════╣╠════════╪═════╪═════╪═════╣
            // ║   0    │     │ s5  │ s4  │     ║║   0    │  1  │  3  │  2  ║
            // ║   1    │     │     │     │ acc ║║   1    │     │     │     ║
            // ║   2    │ s6  │     │     │ r5  ║║   2    │     │     │     ║
            // ║   3    │     │     │     │ r2  ║║   3    │     │     │     ║
            // ║   4    │     │ s5  │ s4  │     ║║   4    │     │  7  │  8  ║
            // ║   5    │ r4  │     │     │ r4  ║║   5    │     │     │     ║
            // ║   6    │     │ s5  │ s4  │     ║║   6    │     │  9  │  8  ║
            // ║   7    │ r3  │     │     │ r3  ║║   7    │     │     │     ║
            // ║   8    │ r5  │     │     │ r5  ║║   8    │     │     │     ║
            // ║   9    │     │     │     │ r1  ║║   9    │     │     │     ║
            // ╚════════╧═════╧═════╧═════╧═════╝╚════════╧═════╧═════╧═════╝
            slrParser.PrintParsingTable(writer);

            // Grammar is not SLR(1)
            slrParser.AnyConflicts.ShouldBeTrue();
            slrParser.Conflicts.Count().ShouldBe(1);

            // This will print
            //      State 2: { shift 6, reduce 5} on 'EQUAL'
            //      State 2: { S → L•EQUAL R, R → L•} (kernel items)
            // The correct choice of the parser is to shift, because no right sentential form begins with....TODO
            // Therefore....TODO
            var conflict = slrParser.Conflicts.Single();
            writer.WriteLine(conflict);
            writer.WriteLine($"State {conflict.State}: {slrParser.GetItems(conflict.State).KernelItems.ToVectorString()} (kernel items)");

            // a=a$
            var lexer = new FakeLexer<Sym>((Sym.ID, "a"), (Sym.EQUAL, "="), (Sym.ID, "a"));
            slrParser.Parse(lexer, writer);
            //  ╔════════╤══════════════╤══════════════╤══════════╤══════════════════════════════════╗
            //  ║ SeqNo  │    Stack     │   Symbols    │  Input   │              Action              ║
            //  ╠════════╪══════════════╪══════════════╪══════════╪══════════════════════════════════╣
            //  ║  (1)   │ 0            │              │   a = a$ │ shift 5                          ║
            //  ║  (2)   │ 0 5          │ ID           │     = a$ │ reduce by L → ID, goto 2         ║
            //  ║  (3)   │ 0 2          │ L            │     = a$ │ shift 6                          ║<--- shift/reduce conflict here (shift wins)
            //  ║  (4)   │ 0 2 6        │ L EQUAL      │       a$ │ shift 5                          ║
            //  ║  (5)   │ 0 2 6 5      │ L EQUAL ID   │        $ │ reduce by L → ID, goto 8         ║
            //  ║  (6)   │ 0 2 6 8      │ L EQUAL L    │        $ │ reduce by R → L, goto 9          ║
            //  ║  (7)   │ 0 2 6 9      │ L EQUAL R    │        $ │ reduce by S → L EQUAL R, goto 1  ║
            //  ║  (8)   │ 0 1          │ S            │        $ │ accept                           ║
            //  ╚════════╧══════════════╧══════════════╧══════════╧══════════════════════════════════╝


            var lr1Parser = grammar.ComputeLr1ParsingTable();

            WriteLine("LR(1) Parsing Table");
            lr1Parser.PrintParsingTable(writer);

            // LR(1) Parsing Table
            // ╔════════════════════════════════╗╔══════════════════════════╗
            // ║             ACTION             ║║           GOTO           ║
            // ╠════════╤═════╤═════╤═════╤═════╣╠════════╤═════╤═════╤═════╣
            // ║ State  │EQUAL│ ID  │ASTER│ EOF ║║ State  │  S  │  R  │  L  ║
            // ╠════════╪═════╪═════╪═════╪═════╣╠════════╪═════╪═════╪═════╣
            // ║   0    │     │ s5  │ s4  │     ║║   0    │  1  │  3  │  2  ║
            // ║   1    │     │     │     │ acc ║║   1    │     │     │     ║
            // ║   2    │ s6  │     │     │ r5  ║║   2    │     │     │     ║
            // ║   3    │     │     │     │ r2  ║║   3    │     │     │     ║
            // ║   4    │     │ s5  │ s4  │     ║║   4    │     │  7  │  8  ║
            // ║   5    │ r4  │     │     │ r4  ║║   5    │     │     │     ║
            // ║   6    │     │ s12 │ s11 │     ║║   6    │     │  9  │ 10  ║
            // ║   7    │ r3  │     │     │ r3  ║║   7    │     │     │     ║
            // ║   8    │ r5  │     │     │ r5  ║║   8    │     │     │     ║
            // ║   9    │     │     │     │ r1  ║║   9    │     │     │     ║
            // ║   10   │     │     │     │ r5  ║║   10   │     │     │     ║
            // ║   11   │     │ s12 │ s11 │     ║║   11   │     │ 13  │ 10  ║
            // ║   12   │     │     │     │ r4  ║║   12   │     │     │     ║
            // ║   13   │     │     │     │ r3  ║║   13   │     │     │     ║
            // ╚════════╧═════╧═════╧═════╧═════╝╚════════╧═════╧═════╧═════╝

            // Grammar is LR(1)
            lr1Parser.AnyConflicts.ShouldBeFalse();

            // a=a$
            var lexer2 = new FakeLexer<Sym>((Sym.ID, "a"), (Sym.EQUAL, "="), (Sym.ID, "a"));
            lr1Parser.Parse(lexer2, writer);
            //  ╔════════╤══════════════╤══════════════╤══════════╤══════════════════════════════════╗
            //  ║ SeqNo  │    Stack     │   Symbols    │  Input   │              Action              ║
            //  ╠════════╪══════════════╪══════════════╪══════════╪══════════════════════════════════╣
            //  ║  (1)   │ 0            │              │     a=a$ │ shift 5                          ║
            //  ║  (2)   │ 0 5          │ ID           │      =a$ │ reduce by L → ID, goto 2         ║
            //  ║  (3)   │ 0 2          │ L            │      =a$ │ shift 6                          ║<--- no reduce, '=' is not a valid lookahead
            //  ║  (4)   │ 0 2 6        │ L EQUAL      │       a$ │ shift 12                         ║
            //  ║  (5)   │ 0 2 6 12     │ L EQUAL ID   │        $ │ reduce by L → ID, goto 10        ║
            //  ║  (6)   │ 0 2 6 10     │ L EQUAL L    │        $ │ reduce by R → L, goto 9          ║
            //  ║  (7)   │ 0 2 6 9      │ L EQUAL R    │        $ │ reduce by S → L EQUAL R, goto 1  ║
            //  ║  (8)   │ 0 1          │ S            │        $ │ accept                           ║
            //  ╚════════╧══════════════╧══════════════╧══════════╧══════════════════════════════════╝


            // TODO: Compute LALR(1) parser
            //      - brute force merging: LR(1) -> LALR(1)
            //      - fixed-point algorithm of propagated lookaheads: LR(0) -> SLR(1) extended follow sets --> LALR(1)


        }

    }
}
