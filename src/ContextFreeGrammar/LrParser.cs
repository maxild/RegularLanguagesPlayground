using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using AutomataLib;

namespace ContextFreeGrammar
{
    // TODO: Maybe rename to ParseTable (SLR-ParsingTable, LALR-ParsingTable etc.)
    /// <summary>
    /// A specialization of a Deterministic Pushdown Automaton (DPDA)
    /// </summary>
    public class LrParser<TNonterminalSymbol, TTerminalSymbol>
        where TNonterminalSymbol : Symbol, IEquatable<TNonterminalSymbol>
        where TTerminalSymbol : Symbol, IEquatable<TTerminalSymbol>
    {
        private readonly Grammar<TNonterminalSymbol, TTerminalSymbol> _grammar;

        private readonly ProductionItemSet<TNonterminalSymbol>[] _originalStates; // TODO: behoever vi den? Den beskriver spelling property of each state

        private readonly Dictionary<TNonterminalSymbol, int> _nonterminalToIndex;
        private readonly TNonterminalSymbol[] _indexToNonterminal;

        private readonly Dictionary<TTerminalSymbol, int> _terminalToIndex;
        private readonly TTerminalSymbol[] _indexToTerminal;

        // no specific error state, because
        private readonly int _maxState;                 // state 0 is error state, other states are numbered 1,2,...,maxState
        private readonly int[,] _gotoTable;             // state 0 is error state in GOTO table
        private readonly LrAction[,] _actionTable;      // LrAction.Error is error state in ACTION table
        private readonly int _start;

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
            _maxState = _originalStates.Length; // 0,1,2,...,maxState, where dead state is at index zero

            // renaming all LR(0) item sets to integer states
            var indexMap = new Dictionary<ProductionItemSet<TNonterminalSymbol>, int>(_maxState); // dead state excluded here
            int stateIndex = 1;
            foreach (ProductionItemSet<TNonterminalSymbol> state in _originalStates)
            {
                indexMap.Add(state, stateIndex);
                stateIndex += 1;
            }

            _start = indexMap[startState];

            // Grammar variables
            _indexToNonterminal = nonterminalSymbols.ToArray();
            _nonterminalToIndex= new Dictionary<TNonterminalSymbol, int>();
            for (int i = 0; i < _indexToNonterminal.Length; i++)
            {
                _nonterminalToIndex[_indexToNonterminal[i]] = i;
            }

            // Grammar tokens (terminals)
            _indexToTerminal = terminalSymbols.ToArray();
            _terminalToIndex= new Dictionary<TTerminalSymbol, int>();
            for (int i = 0; i < _indexToTerminal.Length; i++)
            {
                _terminalToIndex[_indexToTerminal[i]] = i;
            }

            _actionTable = new LrAction[_maxState + 1, _terminalToIndex.Count];

            // Reduce actions differ between different LR methods (SLR strategy uses FOLLOW(A) below)
            foreach (ProductionItemSet<TNonterminalSymbol> itemSet in _originalStates)
            {
                // If A → α"." is in LR(0) item set, then set action[s, a] to 'reduce A → α"."'
                // for all a in T               (LR(0) table)
                // for all a in FOLLOW(A)       (SLR(1) table)
                if (itemSet.IsReduceAction)
                {
                    ProductionItem<TNonterminalSymbol> reduceItem = itemSet.ReduceItems.Single(); // Fail on reduce-reduce conflicts
                    foreach (var terminal in grammar.Terminals)
                    //foreach (var terminal in grammar.FOLLOW(reduceItem.Production.Head))
                    {
                        var symbolIndex = _terminalToIndex[terminal];
                        _actionTable[indexMap[itemSet], symbolIndex] = LrAction.Reduce(reduceItem.ProductionIndex);
                    }
                }

                // TODO: Grammar must be configured with S' → S$ rule
                // If S' → S"." is in LR(0) item set, then set action[s, a] to accept
                if (itemSet.IsAcceptAction)
                {
                    var a = (TTerminalSymbol) itemSet.SpellingSymbol;
                    Debug.Assert(a.Equals(Symbol.Eof<TTerminalSymbol>()));
                    _actionTable[indexMap[itemSet], _terminalToIndex[a]] = LrAction.Accept;
                }
            }

            _gotoTable = new int[_maxState + 1, _nonterminalToIndex.Count];

            // TODO: kan det goeres smartere uden foerst at danne transition triple array

            // Shift and Goto actions (directly from the transitions of the LR(0) automaton)
            foreach (var move in transitions)
            {
                int source = indexMap[move.SourceState];
                int target = indexMap[move.TargetState];

                //if (move.TargetState.IsAcceptAction)
                //{
                //    var a = (TTerminalSymbol) move.Label;
                //    Debug.Assert(a.Equals(Symbol.Eof<TTerminalSymbol>()));
                //    _actionTable[source, _terminalToIndex[a]] = LrAction.Accept;
                //}
                if (move.Label.IsTerminal)
                {
                    // If A → α"."aβ is in LR(0) item set, where a is a terminal symbol
                    var a = (TTerminalSymbol) move.Label;
                    _actionTable[source, _terminalToIndex[a]] = LrAction.Shift(target);
                }
                else
                {
                    // If A → α"."Xβ is in LR(0) item set, where X is a nonterminal symbol
                    var X = (TNonterminalSymbol) move.Label;
                    _gotoTable[source, _nonterminalToIndex[X]]= target;
                }
            }
        }

        enum LrActionKind
        {
            Error = 0,
            Shift,          // Si, where i is a state
            Reduce,         // Rj, where j is production index
            Accept
        }

        [DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
        struct LrAction
        {
            private string DebuggerDisplay => ToString();

            private readonly LrActionKind _kind;
            private readonly int _value;

            public static readonly LrAction Error = new LrAction(LrActionKind.Error, 0);

            public static readonly LrAction Accept = new LrAction(LrActionKind.Accept, 0);

            public static LrAction Reduce(int productionIndex)
            {
                return new LrAction(LrActionKind.Reduce, productionIndex);
            }

            public static LrAction Shift(int stateIndex)
            {
                return new LrAction(LrActionKind.Shift, stateIndex);
            }

            private LrAction(LrActionKind kind, int value)
            {
                _kind = kind;
                _value = value;
            }

            public bool IsShift => _kind == LrActionKind.Shift;
            public bool IsReduce => _kind == LrActionKind.Reduce;
            public bool IsAccept => _kind == LrActionKind.Accept;
            public bool IsError => _kind == LrActionKind.Error;

            public int ShiftTo => _value;

            public Production<TNonterminalSymbol> ReduceTo(Grammar<TNonterminalSymbol, TTerminalSymbol> g) => g.Productions[_value];

            public override string ToString()
            {
                switch (_kind)
                {
                    case LrActionKind.Shift:
                        return $"s{_value}";
                    case LrActionKind.Reduce:
                        return $"r{_value}";
                    default:
                        return _kind.ToString().ToLower();
                }
            }
        }

        // Driver program here
        public void Parse(string input)
        {
            LrAction Action(int state, TTerminalSymbol token)
            {
                return _actionTable[state, _terminalToIndex[token]];
            }

            int Goto(int state, TNonterminalSymbol variable)
            {
                return _gotoTable[state, _nonterminalToIndex[variable]];
            }

            TTerminalSymbol GetNextToken(IEnumerator<TTerminalSymbol> tokenizer)
            {
                return tokenizer.TryGetNext() ?? Symbol.Eof<TTerminalSymbol>();
            }

            int s0 = _start;

            // stack of states (each state uniquely identifies a symbol, such that each
            // configuration (s(0)s(1)...s(m), a(i)a(i+1)...a(n)$) of the parser can generate a
            // right sentential form X(1)X(2)...X(m)a(i+1)...a(n)$). That is X(i) is the grammar
            // symbol associated with state s(i), i > 0. Note the s(0) is the only state not associated
            // with a grammar symbol, because this state represents the initial state og the LR(0) automaton
            // and its role is as a bottom-of-stack marker we can use to accept the parsed string.

            var stack = new Stack<int>();

            // push initial state onto the stack
            stack.Push(s0);

            using (IEnumerator<TTerminalSymbol> tokenizer =
                Letterizer<TTerminalSymbol>.Default.GetLetters(input).GetEnumerator())
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
                        int t = action.ShiftTo;
                        stack.Push(t);
                        Console.WriteLine("shift");
                        // call yylex to get the next token
                        a = GetNextToken(tokenizer);
                    }
                    // Action(s, a) = reduce A → β (DFA recognized a handle)
                    else if (action.IsReduce) // remaining input remains unchanged
                    {
                        Production<TNonterminalSymbol> p = action.ReduceTo(_grammar);
                        // pop |β| symbols off the stack
                        stack.PopItemsOfLength(p.Length);
                        // let state t now be on top of the stack
                        int t = stack.Peek();
                        // push GOTO(t, A) onto the stack
                        int v = Goto(t, p.Head);
                        stack.Push(v);
                        // output the production A → β
                        Console.WriteLine($"reduce by {p}"); // TODO: Create a new AST node for the (semantic) rule A → β, and build AST
                    }
                    // DFA recognized a the accept handle of the initial item set
                    else if (action.IsAccept) // TODO: Blank table cell, maybe use sparse matrices, or adjacency lists
                    {
                        Console.WriteLine("accept");
                        break;
                    }
                    else
                    {
                        // error
                        throw new InvalidOperationException($"Unexpected symbol: {a.Name}");
                    }
                }
            }
        }

        public override string ToString()
        {
            // TODO
            return null;
        }
    }
}
