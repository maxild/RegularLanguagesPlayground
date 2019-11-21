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
        public static void PrintItems<TNonterminalSymbol, TTerminalSymbol>(
            this Dfa<ProductionItemSet<TNonterminalSymbol, TTerminalSymbol>, Symbol> dfa,
            TextWriter writer
        )
            where TNonterminalSymbol : Symbol, IEquatable<TNonterminalSymbol>
            where TTerminalSymbol : Symbol, IEquatable<TTerminalSymbol>
        {
            dfa.PrintItemsHelper(writer, itemSet => itemSet.Items);
        }

        public static void PrintCoreItems<TNonterminalSymbol, TTerminalSymbol>(
            this Dfa<ProductionItemSet<TNonterminalSymbol, TTerminalSymbol>, Symbol> dfa,
            TextWriter writer
            )
            where TNonterminalSymbol : Symbol, IEquatable<TNonterminalSymbol>
            where TTerminalSymbol : Symbol, IEquatable<TTerminalSymbol>
        {
            dfa.PrintItemsHelper(writer, itemSet => itemSet.CoreItems);
        }

        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        private static void PrintItemsHelper<TNonterminalSymbol, TTerminalSymbol>(
            this Dfa<ProductionItemSet<TNonterminalSymbol, TTerminalSymbol>, Symbol> dfa,
            TextWriter writer,
            Func<ProductionItemSet<TNonterminalSymbol, TTerminalSymbol>, IEnumerable<ProductionItem<TNonterminalSymbol, TTerminalSymbol>>> itemsResolver
        )
            where TNonterminalSymbol : Symbol, IEquatable<TNonterminalSymbol>
            where TTerminalSymbol : Symbol, IEquatable<TTerminalSymbol>
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
                foreach (var coreItem in items.Skip(1))
                {
                    writer.Write(new string(' ', maximalStateIndex.Length));
                    writer.WriteLine(coreItem);
                }
            }
        }

        /// <summary>
        /// Print First and Follow sets for all non-terminal symbols.
        /// </summary>
        public static void PrintFirstAndFollowSets<TNonterminalSymbol, TTerminalSymbol>(
            this Grammar<TNonterminalSymbol, TTerminalSymbol> grammar,
            TextWriter writer
            )
            where TNonterminalSymbol : Symbol, IEquatable<TNonterminalSymbol>
            where TTerminalSymbol : Symbol, IEquatable<TTerminalSymbol>
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
            foreach (TNonterminalSymbol variable in grammar.Variables)
            {
                tableWriter.WriteRow(variable.Name,
                    grammar.Erasable(variable).FormatBoolean(),
                    grammar.First(variable).ToVectorString(),
                    grammar.Follow(variable).ToVectorString());
            }
            tableWriter.WriteFooter();
        }

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
