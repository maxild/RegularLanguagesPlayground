using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using AutomataLib;
using AutomataLib.Tables;

namespace ContextFreeGrammar
{
    // The general idea of the parsing algorithm is to match the input that has been seen with the right hand sides
    // of grammar productions until an appropriate match is found, to replace it with the corresponding left hand side.
    // In order to "match the input until an appropriate match is found" the parser saves the grammar symbols it has found
    // so far on its stack. A shift action takes the next token from the input and pushes it onto the stack. A reduce action
    // happens when the sequence of grammar symbols on top of the parsers stack matches the right hand side X1X2...Xn
    // of an appropriate grammar production
    //
    //      A → X1X2...Xn
    //
    // and the parser decides to "reduce" this to the left hand side of the production, i.e., pop the string X1X2...Xn
    // off the stack and push A instead. a shift action means that the parser is waiting to see more of the input before
    // it decides that a match has been found; a reduce action means that a match has been found.
    //
    // There are two types of conflics we might encounter: shift-reduce and reduce-reduce:
    //
    //  1. Shift-reduce conflict. This occurs when the parser is faced with a choice of a shift action and a reduce action.
    //     (Yacc’s default action in the case of a shift-reduce conflict is to choose the shift action.)
    //  2. Reduce-reduce conflict. This occurs when the parser is faced with a choice of two different productions
    //     that could be used for a reduce action. (Yacc’s default action in the case of a reduce-reduce conflict is
    //     to reduce using the production that comes first, textually, in the input grammar specification.)
    //
    // In order to figure out the reason for a conflict, we have to find out (1) which LR(0) items (state) have a conflicts;
    // and (2) the reason for the conflict.

    // TODO: Maybe rename to ParseTable (SLR-ParsingTable, LALR-ParsingTable etc.)
    /// <summary>
    /// A specialization of a Deterministic Pushdown Automaton (DPDA) called a shift/reduce parser in compiler theory.
    /// </summary>
    public class LrParser<TNonterminalSymbol, TTerminalSymbol> : IShiftReduceParser<TNonterminalSymbol, TTerminalSymbol>
        where TNonterminalSymbol : Symbol, IEquatable<TNonterminalSymbol>
        where TTerminalSymbol : Symbol, IEquatable<TTerminalSymbol>
    {
        private readonly Grammar<TNonterminalSymbol, TTerminalSymbol> _grammar;

        // NOTE: This a sort of an adjacency matrix implementation of the two tables (ACTION and GOTO)
        //       where symbols (terminals and nonterminals) are translated to indices via hash tables
        //       (dictionaries), and we explicit values for error transitions, that are given by
        //              GOTO table errors = 0 value of type 'int'
        //              ACTION table errors = LrAction.Error value of type 'LrAction'

        private readonly ProductionItemSet<TNonterminalSymbol>[] _originalStates;

        private readonly Dictionary<TNonterminalSymbol, int> _nonterminalToIndex;
        //private readonly TNonterminalSymbol[] _indexToNonterminal;

        private readonly Dictionary<TTerminalSymbol, int> _terminalToIndex;
        //private readonly TTerminalSymbol[] _indexToTerminal;

        private readonly int _maxState;
        private readonly int[,] _gotoTable;
        private readonly LrAction[,] _actionTable;
        // Conflicts arise from ambiguities in the grammar.
        private readonly Dictionary<(int, TTerminalSymbol), LrConflict<TTerminalSymbol>> _conflictTable =
            new Dictionary<(int, TTerminalSymbol), LrConflict<TTerminalSymbol>>();

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public LrParser(
            Grammar<TNonterminalSymbol, TTerminalSymbol> grammar,
            IEnumerable<ProductionItemSet<TNonterminalSymbol>> states,
            IEnumerable<TNonterminalSymbol> nonterminalSymbols,
            IEnumerable<TTerminalSymbol> terminalSymbols,
            IEnumerable<Transition<Symbol, ProductionItemSet<TNonterminalSymbol>>> transitions,
            ProductionItemSet<TNonterminalSymbol> startState
            )
        {
            _grammar = grammar;

            _originalStates = states.ToArray();

            // no dead state, because we have explicit error action in ACTION table
            // and state 0 is the canonical S' → S production LR(0) item set that can
            // never be reached
            _maxState = _originalStates.Length - 1;

            // TODO: The initial state should be the first state
            // renaming all LR(0) item sets to integer states
            var indexMap = new Dictionary<ProductionItemSet<TNonterminalSymbol>, int>(capacity: _maxState);
            int stateIndex = 0;
            foreach (ProductionItemSet<TNonterminalSymbol> state in _originalStates)
            {
                indexMap.Add(state, stateIndex);
                stateIndex += 1;
            }

            StartState = indexMap[startState];

            // TODO: The initial state should be the first state
            Debug.Assert(StartState == 0);

            // Grammar variables (nonterminals)
            var indexToNonterminal = nonterminalSymbols.ToArray();
            _nonterminalToIndex= new Dictionary<TNonterminalSymbol, int>();
            for (int i = 0; i < indexToNonterminal.Length; i++)
            {
                _nonterminalToIndex[indexToNonterminal[i]] = i;
            }

            // Grammar tokens (terminals)
            var indexToTerminal = terminalSymbols.ToArray();
            _terminalToIndex= new Dictionary<TTerminalSymbol, int>();
            for (int i = 0; i < indexToTerminal.Length; i++)
            {
                _terminalToIndex[indexToTerminal[i]] = i;
            }

            // If EOF ($) is not defined as a valid token then we define it
            if (!_terminalToIndex.ContainsKey(Symbol.Eof<TTerminalSymbol>()))
            {
                _terminalToIndex[Symbol.Eof<TTerminalSymbol>()] = indexToTerminal.Length;
            }

            _actionTable = new LrAction[_maxState + 1, _terminalToIndex.Count];

            // The augmented start variable S' can be excluded from the GOTO table,
            // and we therefore add one less nonterminal symbols to the GOTO table
            _gotoTable = new int[_maxState + 1, _nonterminalToIndex.Count - 1];

            // NOTE: Important that shift actions are configured before reduce actions (conflict resolution)

            // TODO: kan det goeres smartere uden foerst at danne transition triple array
            // Shift and Goto actions (directly from the transitions of the LR(0) automaton)
            foreach (var move in transitions)
            {
                int source = indexMap[move.SourceState];
                int target = indexMap[move.TargetState];

                if (move.Label.IsTerminal)
                {
                    // If A → α"."aβ is in LR(0) item set, where a is a terminal symbol
                    var a = (TTerminalSymbol) move.Label;
                    var symbolIndex = _terminalToIndex[a];
                    _actionTable[source, symbolIndex] = LrAction.Shift(target);
                }
                else
                {
                    // If A → α"."Xβ is in LR(0) item set, where X is a nonterminal symbol
                    var X = (TNonterminalSymbol) move.Label;
                    int symbolIndex = _nonterminalToIndex[X];
                    _gotoTable[source, symbolIndex - 1] = target;
                }
            }

            // Reduce actions differ between different LR methods (SLR strategy uses FOLLOW(A) below)
            foreach (ProductionItemSet<TNonterminalSymbol> itemSet in _originalStates)
            {
                // If A → α"." is in LR(0) item set, then set action[s, a] to 'reduce A → α"."' (where A is not S')
                // for all a in T               (LR(0) table)
                // for all a in FOLLOW(A)       (SLR(1) table)
                if (itemSet.IsReduceAction)
                {
                    foreach (ProductionItem<TNonterminalSymbol> reduceItem in
                             itemSet.ReduceItems.OrderBy(item => item.ProductionIndex)) // choose reductions with lowest index
                    {
                        var state = indexMap[itemSet];

                        // LR(0) grammar rule
                        foreach (var terminal in grammar.Terminals)
                        // SLR(1) grammar rule
                        //foreach (var terminal in grammar.FOLLOW(reduceItem.Production.Head))
                        {
                            var symbolIndex = _terminalToIndex[terminal];
                            var reduceAction = LrAction.Reduce(reduceItem.ProductionIndex);
                            // error is the default action, and not an error will indicate a conflict
                            if (!_actionTable[state, symbolIndex].IsError)
                            {
                                // report diagnostic for the found shift/reduce or reduce/reduce conflict
                                if (_conflictTable.ContainsKey((state, terminal)))
                                {
                                    _conflictTable[(state, terminal)] = _conflictTable[(state, terminal)]
                                        .WithAction(reduceAction);
                                }
                                else
                                {
                                    _conflictTable.Add((state, terminal), new LrConflict<TTerminalSymbol>(
                                        state,
                                        terminal,
                                        new[]
                                        {
                                            _actionTable[state, symbolIndex],   // existing (shift or reduce) action
                                            reduceAction                        // reduce action
                                        }));
                                }

                                // choose the shift action (for any shift/reduce conflicts), or choose the reduce action
                                // with the lowest index (for any reduce/reduce conflicts), by ignoring to put the reduce action in
                                // the table (Yacc, Happy etc. all choose this strategy for resolving conflicts)
                            }
                            else
                            {
                                _actionTable[state, symbolIndex] = reduceAction;
                            }
                        }
                    }
                }

                // If S' → S"." is in LR(0) item set, then set action[s, $] to accept
                if (itemSet.IsAcceptAction)
                {
                    // NOTE: Only if the grammar is augmented with S' → S$ (i.e. EOF marker added
                    // to augmented rule) then we can be sure that the accept action item set has
                    // the EOF marked symbol ($) as the spelling property.
                    //      Debug.Assert(itemSet.SpellingSymbol.Equals(Symbol.Eof<TTerminalSymbol>()));
                    int eofIndex = _terminalToIndex[Symbol.Eof<TTerminalSymbol>()];
                    _actionTable[indexMap[itemSet], eofIndex] = LrAction.Accept; // we cannot have a conflict here
                }
            }
        }

        public int StartState { get; }

        public TNonterminalSymbol StartSymbol => _grammar.Productions[0].Head;

        public ProductionItemSet<TNonterminalSymbol> GetItems(int state)
        {
            return _originalStates[state];
        }

        public IEnumerable<int> GetStates()
        {
            return Enumerable.Range(0, _maxState + 1);  // 0, 1, 2,...,maxState-1
        }

        public IEnumerable<TTerminalSymbol> TerminalSymbols => _terminalToIndex.Keys;

        public IEnumerable<TNonterminalSymbol> NonTerminalSymbols => _nonterminalToIndex.Keys;

        /// <summary>
        /// Nonterminal symbols, excluding the augmented start symbol.
        /// </summary>
        public IEnumerable<TNonterminalSymbol> TrimmedNonTerminalSymbols => _nonterminalToIndex.Keys.Where(symbol => !StartSymbol.Equals(symbol));

        public LrAction Action(int state, TTerminalSymbol token)
        {
            return _actionTable[state, _terminalToIndex[token]];
        }

        public int Goto(int state, TNonterminalSymbol variable)
        {
            int symbolIndex = _nonterminalToIndex[variable];
            return symbolIndex == 0 ? 0 : _gotoTable[state, symbolIndex - 1];
        }

        /// <summary>
        /// If the grammar is ambiguous, then we have found some conflicts in the parsing table.
        /// </summary>
        public bool IsAmbiguous => _conflictTable.Count > 0;

        /// <summary>
        /// The sequence of found conflicts.
        /// </summary>
        public IEnumerable<LrConflict<TTerminalSymbol>> Conflicts => _conflictTable.Values;

        // Driver program here
        public void Parse(string input, TextWriter logger = null)
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
                    new Column("Action", 24, Align.Left))
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
            stack.Push(StartState);

            using (IEnumerator<TTerminalSymbol> tokenizer = ((IEnumerable<TTerminalSymbol>) inputSymbolArray).GetEnumerator())
            {
                TTerminalSymbol a = GetNextToken(tokenizer);
                while (true)
                {
                    int s = stack.Peek();
                    var action = Action(s, a);
                    // Action(s, a) = shift t
                    if (action.IsShift) // consume input token here
                    {
                        // push t onto the stack
                        int t = action.ShiftToState;

                        // output 'shift t'
                        tableWriter?.WriteRow($"({seqNo++})",
                            $" {string.Join(" ", stack.Reverse())}",
                            $" {string.Join(" ", stack.Reverse().Select(state => _originalStates[state].SpellingSymbol?.Name ?? string.Empty))}",
                            $"{GetStringOfSpan(inputSymbols.Slice(ip++))} ",
                            $" shift {t}");

                        stack.Push(t);

                        // call yylex to get the next token
                        a = GetNextToken(tokenizer);
                    }
                    // Action(s, a) = reduce A → β (DFA recognized a handle)
                    else if (action.IsReduce) // remaining input remains unchanged
                    {
                        Production<TNonterminalSymbol> p = _grammar.Productions[action.ReduceToProductionIndex];

                        // output 'reduce by A → β'
                        tableWriter?.WriteRow($"({seqNo++})",
                            $" {string.Join(" ", stack.Reverse())}",
                            $" {string.Join(" ", stack.Reverse().Select(state => _originalStates[state].SpellingSymbol?.Name ?? string.Empty))}",
                            $"{GetStringOfSpan(inputSymbols.Slice(ip))} ",
                            $" reduce by {p}");

                        // pop |β| symbols off the stack
                        stack.PopItemsOfLength(p.Length);
                        // let state t now be on top of the stack
                        int t = stack.Peek();
                        // push GOTO(t, A) onto the stack
                        int v = Goto(t, p.Head);
                        stack.Push(v);

                        // TODO: Create a new AST node for the (semantic) rule A → β, and build AST
                    }
                    // DFA recognized a the accept handle of the initial item set
                    else if (action.IsAccept)
                    {
                        // output accept
                        tableWriter?.WriteRow($"({seqNo})",
                            $" {string.Join(" ", stack.Reverse())}",
                            $" {string.Join(" ", stack.Reverse().Select(state => _originalStates[state].SpellingSymbol?.Name ?? string.Empty))}",
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
