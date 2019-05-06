using System;
using AutomataLib;
using ContextFreeGrammar;
using Xunit;

namespace UnitTests
{
    public class TableTests
    {
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
                Symbol.V("S").GoesTo(Symbol.V("E"), Symbol.Eof<Terminal>()),
                Symbol.V("E").GoesTo(Symbol.V("E"), Symbol.T('+'), Symbol.V("T")),
                Symbol.V("E").GoesTo(Symbol.V("T")),
                Symbol.V("T").GoesTo(Symbol.V("T"), Symbol.T('*'), Symbol.V("F")),
                Symbol.V("T").GoesTo(Symbol.V("F")),
                Symbol.V("F").GoesTo(Symbol.T('('), Symbol.V("E"), Symbol.T(')')),
                Symbol.V("F").GoesTo(Symbol.T('-'), Symbol.V("T")),
                Symbol.V("F").GoesTo(Symbol.T('a'))
            };

            var table = new TableBuilder()
                .SetTitle("MyTitle")
                .SetColumns(new Column("Variable", 12), new Column("Nullable", 12), new Column("First", 12),
                    new Column("Follow", 12))
                .Build();

            // TODO: Hvor ryger den hen vha xunit???
            var tableWriter = new TextTableWriter(table, Console.Out);
            tableWriter.WriteHead();
            foreach (Nonterminal variable in grammar.Variables)
            {
                tableWriter.WriteRow(variable.Name,
                    grammar.NULLABLE(variable).FormatBoolean(),
                    grammar.FIRST(variable).ToVectorString(),
                    grammar.FOLLOW(variable).ToVectorString());
            }
            tableWriter.WriteFooter();
        }

        [Fact]
        public void CreateSlrTable()
        {
            // 0: S → E
            // 1: E → E + T
            // 2: E → T
            // 3: T → T * F
            // 4: T → F
            // 5: T → (E)
            // 6: T → a        (Dragon book has 'id' terminal here, but our model only supports single char tokens at the moment)
            var grammar = new Grammar(
                variables: Symbol.Vs("S", "E", "T", "F"),
                terminals: Symbol.Ts('a', '+', '*', '(', ')').WithEofMarker(),
                startSymbol: Symbol.V("S"))
            {
                Symbol.V("S").GoesTo(Symbol.V("E"), Symbol.Eof<Terminal>()),
                Symbol.V("E").GoesTo(Symbol.V("E"), Symbol.T('+'), Symbol.V("T")),
                Symbol.V("E").GoesTo(Symbol.V("T")),
                Symbol.V("T").GoesTo(Symbol.V("T"), Symbol.T('*'), Symbol.V("F")),
                Symbol.V("T").GoesTo(Symbol.V("F")),
                Symbol.V("F").GoesTo(Symbol.T('('), Symbol.V("E"), Symbol.T(')')),
                Symbol.V("F").GoesTo(Symbol.T('a'))
            };

            // Action table
            var table = new TableBuilder()
                .SetTitle("ACTION")
                .Build();

            //table.Columns.AddRange


        }
    }
}
