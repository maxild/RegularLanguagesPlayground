using System;
using System.IO;
using System.Linq;
using AutomataLib;
using AutomataLib.Tables;

namespace ContextFreeGrammar
{
    public static class ParsingTablePrinter
    {
        public static void PrintParsingTable<TNonterminalSymbol, TTerminalSymbol>(
            this IShiftReduceParser<TNonterminalSymbol, TTerminalSymbol> parser,
            TextWriter writer
            )
            where TNonterminalSymbol : Symbol, IEquatable<TNonterminalSymbol>
            where TTerminalSymbol : Symbol, IEquatable<TTerminalSymbol>
        {
            var actionTable = new TableBuilder()
                .SetTitle("ACTION")
                .SetColumns(new Column("State", 8).AsSingletonEnumerable()
                    .Concat(parser.TerminalSymbols.Select(token => new Column(token.Name, 5))))
                .Build();

            var actionTableWriter = new TextTableWriter(actionTable, writer);
            actionTableWriter.WriteHead();
            foreach (var state in parser.GetStates())
            {
                actionTableWriter.WriteRow(state.ToString().AsSingletonEnumerable()
                    .Concat(parser.TerminalSymbols.Select(token => parser.Action(state, token).ToTableString())).ToArray());
            }

            actionTableWriter.WriteFooter();

            writer.WriteLine();
            writer.WriteLine();

            var gotoTable = new TableBuilder()
                .SetTitle("GOTO")
                .SetColumns(new Column("State", 8).AsSingletonEnumerable()
                    .Concat(parser.TrimmedNonTerminalSymbols.Select(nonterminal => new Column(nonterminal.Name, 5))))
                .Build();

            var gotoTableWriter = new TextTableWriter(gotoTable, writer);
            gotoTableWriter.WriteHead();
            foreach (var state in parser.GetStates())
            {
                gotoTableWriter.WriteRow(state.ToString().AsSingletonEnumerable()
                    .Concat(parser.TrimmedNonTerminalSymbols.Select(nonterminal => parser.Goto(state, nonterminal).ToGotoTableString())).ToArray());
            }

            gotoTableWriter.WriteFooter();
        }
    }
}
