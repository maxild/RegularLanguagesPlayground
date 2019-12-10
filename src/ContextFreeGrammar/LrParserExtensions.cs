using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using AutomataLib;
using AutomataLib.Tables;
using ContextFreeGrammar.Lexers;

namespace ContextFreeGrammar
{
    public static class LrParserExtensions
    {
        // Driver program here
        public static void Parse<TTokenKind>(
            this IShiftReduceParsingTable<TTokenKind> parser,
            ILexer<Token<TTokenKind>> lexer,
            TextWriter logger = null
        ) where TTokenKind : Enum
        {
            var table = new TableBuilder()
                .SetColumns(new Column("SeqNo", 8),
                    new Column("Stack", 14, Align.Left),
                    new Column("Symbols", 14, Align.Left),
                    new Column("Input", 10, Align.Right),
                    new Column("Action", 34, Align.Left))
                .Build();

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

            // TODO: Are we using token (lexer type) or terminal (grammar type) here
            Token<TTokenKind> token = lexer.GetNextToken();
            Terminal<TTokenKind> a = Symbol.T(token.Kind);

            while (true)
            {
                int s = stack.Peek();
                var action = parser.Action(s, a);
                // Action(s, a) = shift t
                if (action.IsShift) // consume input token here
                {
                    // push t onto the stack
                    int t = action.ShiftToState;

                    if (lexer is IBufferedLexer<Token<TTokenKind>> fake)
                    {
                        // output 'shift t'
                        tableWriter?.WriteRow($"({seqNo++})",
                            $" {string.Join(" ", stack.Reverse())}",
                            $" {string.Join(" ", stack.Reverse().Skip(1).Select(state => parser.GetItems(state).SpellingSymbol.Name))}",
                            $"{fake.GetRemainingTokens(ip++).Aggregate(new StringBuilder(), (sb, tok) => sb.Append(tok.Text))} ",
                            $" shift {t}");
                    }
                    else
                    {
                        // output 'shift t'
                        tableWriter?.WriteRow($"({seqNo++})",
                            $" {string.Join(" ", stack.Reverse())}",
                            $" {string.Join(" ", stack.Reverse().Skip(1).Select(state => parser.GetItems(state).SpellingSymbol.Name))}",
                            " ",
                            $" shift {t}");
                    }

                    stack.Push(t);

                    // call yylex to get the next token
                    // TODO: Are we using token (lexer type) or terminal (grammar type) here
                    token = lexer.GetNextToken();
                    a = Symbol.T(token.Kind);

                }
                // Action(s, a) = reduce A → β (DFA recognized a handle)
                else if (action.IsReduce) // remaining input remains unchanged
                {
                    Production p = parser.Productions[action.ReduceToProductionIndex];

                    // output 'reduce by A → β'
                    string[] values = null;
                    if (tableWriter != null)
                    {
                        if (lexer is IBufferedLexer<Token<TTokenKind>> fake)
                        {
                            values = new[]
                            {
                                $"({seqNo++})",
                                $" {string.Join(" ", stack.Reverse())}",
                                $" {string.Join(" ", stack.Reverse().Skip(1).Select(state => parser.GetItems(state).SpellingSymbol.Name))}",
                                $"{fake.GetRemainingTokens(ip).Aggregate(new StringBuilder(), (sb, tok) => sb.Append(tok.Text))} ",
                                $" reduce by {p}"
                            };
                        }
                        else
                        {
                            values = new[]
                            {
                                $"({seqNo++})",
                                $" {string.Join(" ", stack.Reverse())}",
                                $" {string.Join(" ", stack.Reverse().Skip(1).Select(state => parser.GetItems(state).SpellingSymbol.Name))}",
                                " ",
                                $" reduce by {p}"
                            };
                        }
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
                    // TODO: Should this be validated here???
                    Debug.Assert(lexer.GetNextToken().IsEof);

                    if (lexer is IBufferedLexer<Token<TTokenKind>> fake)
                    {
                        // output accept
                        tableWriter?.WriteRow($"({seqNo})",
                            $" {string.Join(" ", stack.Reverse())}",
                            $" {string.Join(" ", stack.Reverse().Skip(1).Select(state => parser.GetItems(state).SpellingSymbol.Name))}",
                            $"{fake.GetRemainingTokens(ip).Aggregate(new StringBuilder(), (sb, tok) => sb.Append(tok.Text))} ",
                            $" {action}");

                    }
                    else
                    {
                        // output accept
                        tableWriter?.WriteRow($"({seqNo})",
                            $" {string.Join(" ", stack.Reverse())}",
                            $" {string.Join(" ", stack.Reverse().Skip(1).Select(state => parser.GetItems(state).SpellingSymbol.Name))}",
                            " ",
                            $" {action}");
                    }

                    break;
                }
                else
                {
                    // error
                    throw new InvalidOperationException($"Unexpected symbol: {a.Name}");
                }
            }

            tableWriter?.WriteFooter();
        }
    }
}
