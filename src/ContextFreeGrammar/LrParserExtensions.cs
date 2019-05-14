using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AutomataLib;
using AutomataLib.Tables;

namespace ContextFreeGrammar
{
    public static class LrParserExtensions
    {
        // Driver program here
        public static void Parse<TNonterminalSymbol, TTerminalSymbol>(
            this IShiftReduceParsingTable<TNonterminalSymbol, TTerminalSymbol> parser,
            string input,
            TextWriter logger = null
        )
            where TNonterminalSymbol : Symbol, IEquatable<TNonterminalSymbol>
            where TTerminalSymbol : Symbol, IEquatable<TTerminalSymbol>
        {
            TTerminalSymbol GetNextToken(IEnumerator<TTerminalSymbol> tokenizer)
            {
                return tokenizer.TryGetNext() ?? Symbol.Eof<TTerminalSymbol>();
            }

            string GetStringOfSpan(Span<TTerminalSymbol> symbols)
            {
                var iter = symbols.GetEnumerator();
                var sb = new StringBuilder();
                while (iter.MoveNext())
                {
                    sb.Append(iter.Current.Name);
                }
                sb.Append(Symbol.EofMarker);
                return sb.ToString();
            }

            var table = new TableBuilder()
                .SetColumns(new Column("SeqNo", 8),
                    new Column("Stack", 14, Align.Left),
                    new Column("Symbols", 14, Align.Left),
                    new Column("Input", 10, Align.Right),
                    new Column("Action", 34, Align.Left))
                .Build();

            // We only need all the following variables because we support logging to a user-defined table writer
            IEnumerable<TTerminalSymbol> inputSymbolSequence = Letterizer<TTerminalSymbol>.Default.GetLetters(input);
            TTerminalSymbol[] inputSymbolArray = inputSymbolSequence.ToArray();
            Span<TTerminalSymbol> inputSymbols = inputSymbolArray.AsSpan();

            int seqNo = 1;
            int ip = 0;
            TableWriter tableWriter = null;
            if (logger != null)
            {
                tableWriter = new TextTableWriter(table, logger);
                tableWriter.WriteHead();
            }

            // stack of states (each state uniquely identifies a symbol, such that each
            // configuration (s(0)s(1)...s(m), a(i)a(i+1)...a(n)$) of the parser can generate a
            // right sentential form X(1)X(2)...X(m)a(i+1)...a(n)$). That is X(i) is the grammar
            // symbol associated with state s(i), i > 0. Note the s(0) is the only state not associated
            // with a grammar symbol, because this state represents the initial state og the LR(0) automaton
            // and its role is as a bottom-of-stack marker we can use to accept the parsed string.
            var stack = new Stack<int>();

            // push initial state onto the stack
            stack.Push(parser.StartState);

            using (IEnumerator<TTerminalSymbol> tokenizer = ((IEnumerable<TTerminalSymbol>) inputSymbolArray).GetEnumerator())
            {
                TTerminalSymbol a = GetNextToken(tokenizer);
                while (true)
                {
                    int s = stack.Peek();
                    var action = parser.Action(s, a);
                    // Action(s, a) = shift t
                    if (action.IsShift) // consume input token here
                    {
                        // push t onto the stack
                        int t = action.ShiftToState;

                        // output 'shift t'
                        tableWriter?.WriteRow($"({seqNo++})",
                            $" {string.Join(" ", stack.Reverse())}",
                            $" {string.Join(" ", stack.Reverse().Skip(1).Select(state => parser.GetItems(state).SpellingSymbol.Name))}",
                            $"{GetStringOfSpan(inputSymbols.Slice(ip++))} ",
                            $" shift {t}");

                        stack.Push(t);

                        // call yylex to get the next token
                        a = GetNextToken(tokenizer);
                    }
                    // Action(s, a) = reduce A → β (DFA recognized a handle)
                    else if (action.IsReduce) // remaining input remains unchanged
                    {
                        Production<TNonterminalSymbol> p = parser.Productions[action.ReduceToProductionIndex];

                        // output 'reduce by A → β'
                        string[] values = null;
                        if (tableWriter != null)
                        {
                            values = new[]
                            {
                                $"({seqNo++})",
                                $" {string.Join(" ", stack.Reverse())}",
                                $" {string.Join(" ", stack.Reverse().Skip(1).Select(state => parser.GetItems(state).SpellingSymbol.Name))}",
                                $"{GetStringOfSpan(inputSymbols.Slice(ip))} ",
                                $" reduce by {p}"
                            };
                        }

                        // pop |β| symbols off the stack
                        stack.PopItemsOfLength(p.Length);
                        // let state t now be on top of the stack
                        int t = stack.Peek();
                        // push GOTO(t, A) onto the stack
                        int v = parser.Goto(t, p.Head);
                        stack.Push(v);

                        if (tableWriter != null)
                        {
                            values[4] += $", goto {v}";
                            tableWriter.WriteRow(values);
                        }

                        // TODO: Create a new AST node for the (semantic) rule A → β, and build AST
                    }
                    // DFA recognized a the accept handle of the initial item set
                    else if (action.IsAccept)
                    {
                        // output accept
                        tableWriter?.WriteRow($"({seqNo})",
                            $" {string.Join(" ", stack.Reverse())}",
                            $" {string.Join(" ", stack.Reverse().Skip(1).Select(state => parser.GetItems(state).SpellingSymbol.Name))}",
                            $"{GetStringOfSpan(inputSymbols.Slice(ip))} ",
                            $" {action}");

                        break;
                    }
                    else
                    {
                        // error
                        throw new InvalidOperationException($"Unexpected symbol: {a.Name}");
                    }
                }
            }

            tableWriter?.WriteFooter();
        }
    }
}
