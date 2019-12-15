using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using AutomataLib;
using AutomataLib.Tables;

namespace ContextFreeGrammar
{
    public static class ParsingTablePrinter
    {
        public static void PrintItems<TTokenKind>(
            this LrItemsDfa<TTokenKind> dfa,
            TextWriter writer
            ) where TTokenKind : struct, Enum
        {
            dfa.PrintItemsHelper(writer, itemSet => itemSet.Items);
        }

        public static void PrintKernelItems<TTokenKind>(
            this LrItemsDfa<TTokenKind> dfa,
            TextWriter writer
            ) where TTokenKind : struct, Enum
        {
            dfa.PrintItemsHelper(writer, itemSet => itemSet.KernelItems);
        }

        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        private static void PrintItemsHelper<TTokenKind>(
            this LrItemsDfa<TTokenKind> dfa,
            TextWriter writer,
            Func<ProductionItemSet<TTokenKind>, IEnumerable<ProductionItem<TTokenKind>>> itemsResolver
            ) where TTokenKind : struct, Enum
        {
            string maximalStateIndex = $"s{dfa.MaxState - 1}: ";

            // Trimmed states are ordered 1,2,3,...,MaxState (because we neglect the dead state at index zero)
            // To be consistent with dragon book we order states 0,1,2,...,MaxState-1 (as if dead state does not exist)
            foreach (int state in dfa.GetTrimmedStates())
            {
                var itemSet = dfa.GetUnderlyingState(state);
                var items = itemsResolver(itemSet);
                string stateIndex = $"s{state - 1}:".PadRight(maximalStateIndex.Length);
                writer.Write(stateIndex);
                writer.WriteLine(items.First());
                foreach (var kernelItem in items.Skip(1))
                {
                    writer.Write(new string(' ', maximalStateIndex.Length));
                    writer.WriteLine(kernelItem);
                }
            }
        }

        /// <summary>
        /// Print First and Follow sets for all non-terminal symbols.
        /// </summary>
        public static void PrintFirstAndFollowSets<TTokenKind>(
            this Grammar<TTokenKind> grammar,
            TextWriter writer
            ) where TTokenKind : struct, Enum
        {
            var table = new TableBuilder()
                .SetTitle("First and Follow sets")
                .SetColumns(new Column("Variable", 12),
                    new Column("Nullable", 12),
                    new Column("First", 16),
                    new Column("Follow", 16))
                .Build();

            var tableWriter = new TextTableWriter(table, writer);
            tableWriter.WriteHead();
            foreach (Nonterminal variable in grammar.Nonterminals)
            {
                tableWriter.WriteRow(variable.Name,
                    grammar.Erasable(variable).FormatBoolean(),
                    grammar.First(variable).ToVectorString(),
                    grammar.Follow(variable).ToVectorString());
            }
            tableWriter.WriteFooter();
        }

        public static void PrintParsingTable<TTokenKind>(
            this IShiftReduceParser<TTokenKind> parser,
            TextWriter writer
            ) where TTokenKind : struct, Enum
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
