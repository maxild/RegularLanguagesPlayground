using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using AutomataLib;

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
    // off the stack and push A instead. A shift action means that the parser is waiting to see more of the input before
    // it decides that a match has been found. A reduce action means that a match has been found. A goto action is a nonterminal
    // transition that happens after each reduction (except when the reduction is the initial S' –> S rule where the parser
    // only checks for end-of-input before it accepts the input).
    //
    // There are two types of conflicts: shift-reduce and reduce-reduce:
    //
    //  1. Shift-reduce conflict. This occurs when the parser is faced with a choice of a shift action and a reduce action.
    //     (Yacc’s default action in the case of a shift-reduce conflict is to choose the shift action.)
    //  2. Reduce-reduce conflict. This occurs when the parser is faced with a choice of two different productions
    //     that could be used for a reduce action. (Yacc’s default action in the case of a reduce-reduce conflict is
    //     to reduce using the production that comes first, textually, in the input grammar specification.)
    //
    // In order to figure out the reason for a conflict, we have to find out (1) which LR(0)/LR(1) item sets have a conflict;
    // and (2) the reason for the conflict.
    //
    // LR(0) Grammar
    // =============
    // A grammar is LR(0) if the following two conditions hold for all LR(0) item sets
    // (aka configurating set):
    //
    //  1. For any LR(0) item set containing the (shift) item A → α•aβ there is no (reduce) item
    //     B –> w• in that set. In the parsing table, this translates to no shift/­reduce conflict
    //     on any state.  This means the successor function from that set either shifts to a new
    //     state or reduces, but not both.
    //  2. There is at most one (reduce) item A –> u• in each item set. This translates to no
    //     reduce/­reduce conflict on any state. The successor function has at most one reduction.
    //
    // SLR(1) Grammar
    // ==============
    // A grammar is SLR(1) if the following two conditions hold for all LR(0) item sets
    // (aka configurating set):
    //
    //  1. For any (shift) item A → α•aβ in the set, with terminal 'a', there is no (reduce) item
    //     B –> w• in that set with 'a' in Follow(B). In the parsing tables, this translates to no
    //     shift/­reduce conflict on any state. This means the successor function for symbol 'a'
    //     from that set either shifts to a new state or reduces, but not both.
    //  2. For any two (reduce) items A –> α• and B –> β• in the set, the follow sets must
    //     be disjoint, e.g. Follow(A) ∩ Follow(B) is empty. This translates to no reduce/­reduce
    //     conflict on any state. If more than one non­terminal can be reduced from this set,
    //     it must be possible to uniquely determine which using only one token of lookahead.
    //
    // LR(1) Grammar
    // ==============
    // A grammar is LR(1) if the following two conditions are satisfied for all LR(1) item sets
    // (aka configurating set):
    //
    //  1. For any (shift) item in the set [A → α•aβ, b] with terminal 'a', there is no (reduce) item
    //     in the set of the form [B –> v•, a]. In the action table, this translates to no shift/reduce
    //     conflict for any state. The successor function for symbol 'a' either shifts to a new state or
    //     reduces, but not both.
    //  2. The lookaheads for all reduce items within the set must be disjoint, e.g. the set
    //     cannot have both [A –> u•, a] and [B –> v•, a]. This translates to no reduce/­reduce
    //     conflict on any state.  If more than one non­terminal could be reduced from this
    //     set, it must be possible to uniquely determine which is appropriate from the next
    //     input token.

    // TODO: Maybe rename to ParseTable (SLR-ParsingTable, LALR-ParsingTable etc.)
    /// <summary>
    /// A specialization of a Deterministic Pushdown Automaton (DPDA) called a shift/reduce parser in compiler theory.
    /// </summary>
    public class LrParser<TTokenKind> : IShiftReduceParser<TTokenKind>
        where TTokenKind : struct, Enum
    {
        private readonly Grammar<TTokenKind> _grammar;

        // NOTE: This is sort of an adjacency matrix implementation of the two tables (ACTION and GOTO)
        //       where symbols (terminals and nonterminals) are translated to indices via hash tables
        //       (dictionaries), and we explicit values for error transitions, that are given by
        //              GOTO table errors = 0 value of type 'int'
        //              ACTION table errors = LrAction.Error value of type 'LrAction'

        private readonly IReadOnlyOrderedSet<ProductionItemSet<TTokenKind>> _originalStates;

        // TODO: Use index properties on grammar symbol instances

        private readonly Dictionary<Nonterminal, int> _nonterminalToIndex;
        //private readonly TNonterminalSymbol[] _indexToNonterminal;

        private readonly Dictionary<Terminal<TTokenKind>, int> _terminalToIndex;
        //private readonly TTerminalSymbol[] _indexToTerminal;

        private readonly int _maxState;
        private readonly int[,] _gotoTable;
        private readonly LrAction[,] _actionTable;

        private readonly Dictionary<(int, Terminal<TTokenKind>), LrConflict<TTokenKind>> _conflictTable =
            new Dictionary<(int, Terminal<TTokenKind>), LrConflict<TTokenKind>>();

        public LrParser(
            Grammar<TTokenKind> grammar,
            IReadOnlyOrderedSet<ProductionItemSet<TTokenKind>> originalStates,
            IEnumerable<Nonterminal> nonterminalSymbols,
            IEnumerable<Terminal<TTokenKind>> terminalSymbols,
            IEnumerable<LrActionEntry<TTokenKind>> actionTableEntries,
            IEnumerable<LrGotoEntry> gotoTableEntries
            )
        {
            // TODO: states are numbered 0,1,...,N-1 when using ordered states set....Why not use DFA ordering 1,...,N in the parse tables???

            _grammar = grammar;
            _originalStates = originalStates;
            _maxState = originalStates.Count - 1;
            StartState = 0;

            // Grammar variables (nonterminals)
            var indexToNonterminal = nonterminalSymbols.ToArray();
            _nonterminalToIndex= new Dictionary<Nonterminal, int>();
            for (int i = 0; i < indexToNonterminal.Length; i++)
                _nonterminalToIndex[indexToNonterminal[i]] = i;

            // Grammar tokens (terminals)
            var indexToTerminal = terminalSymbols.ToArray();
            _terminalToIndex= new Dictionary<Terminal<TTokenKind>, int>();
            for (int i = 0; i < indexToTerminal.Length; i++)
                _terminalToIndex[indexToTerminal[i]] = i;

            // If EOF ($) is not defined as a valid token then we define it
            if (!_terminalToIndex.ContainsKey(Symbol.Eof<TTokenKind>()))
                _terminalToIndex[Symbol.Eof<TTokenKind>()] = indexToTerminal.Length;

            _actionTable = new LrAction[_maxState + 1, _terminalToIndex.Count];

            // The augmented start variable S' can be excluded from the GOTO table,
            // and we therefore add one less nonterminal symbols to the GOTO table
            _gotoTable = new int[_maxState + 1, _nonterminalToIndex.Count - 1];

            // NOTE: Important that shift actions are inserted before reduce
            //       actions among the entries (conflict resolution)

            foreach (var entry in actionTableEntries)
            {
                var symbolIndex = _terminalToIndex[entry.TerminalSymbol];

                // error is the default action, and not an error is a conflict -- (i, j) occupied
                if (!_actionTable[entry.State, symbolIndex].IsError)
                {
                    // Idiomatic Conflict Resolution (Yacc, Happy etc. all choose this strategy for resolving conflicts):
                    //     * shift on shift-reduce conflicts, and
                    //     * reduce using the production that comes first, textually, in the input grammar specification

                    // Only reduce actions can cause conflicts, when all shift actions are inserted first,
                    // because shift-shift conflicts are impossible in the LR(k) automaton.
                    Debug.Assert(entry.Action.IsReduce);

                    // The reduce action in the table is has the lowest production index
                    Debug.Assert(!_actionTable[entry.State, symbolIndex].IsReduce ||
                                 entry.Action.ReduceToProductionIndex > _actionTable[entry.State, symbolIndex].ReduceToProductionIndex);

                    // report diagnostic for the found shift/reduce or reduce/reduce conflict
                    if (_conflictTable.ContainsKey((entry.State, entry.TerminalSymbol)))
                    {
                        // append the (reduce) action to the list of actions of an existing conflict
                        _conflictTable[(entry.State, entry.TerminalSymbol)] =
                            _conflictTable[(entry.State, entry.TerminalSymbol)]
                                .WithAction(entry.Action);
                    }
                    else
                    {
                        // add new conflict
                        _conflictTable.Add((entry.State, entry.TerminalSymbol), new LrConflict<TTokenKind>(
                            entry.State,
                            entry.TerminalSymbol,
                            new[]
                            {
                                _actionTable[entry.State, symbolIndex], // existing (shift or reduce) action
                                entry.Action                            // reduce action
                            }));
                    }
                }
                else
                {
                    _actionTable[entry.State, symbolIndex] = entry.Action;
                }
            }

            foreach (var entry in gotoTableEntries)
            {
                int symbolIndex = _nonterminalToIndex[entry.NonterminalSymbol];
                _gotoTable[entry.SourceState, symbolIndex - 1] = entry.TargetState;
            }
        }

        /// <inheritdoc />
        public int StartState { get; }

        /// <inheritdoc />
        public Nonterminal StartSymbol => _grammar.Productions[StartState].Head;

        /// <inheritdoc />
        public IReadOnlyList<Production> Productions => _grammar.Productions;

        /// <inheritdoc />
        public ProductionItemSet<TTokenKind> GetItems(int state)
        {
            return _originalStates[state];
        }

        /// <inheritdoc />
        public IEnumerable<int> GetStates()
        {
            return Enumerable.Range(0, count: _maxState + 1);  // 0, 1, 2,...,maxState
        }

        /// <inheritdoc />
        public IEnumerable<Terminal<TTokenKind>> TerminalSymbols => _terminalToIndex.Keys;

        /// <inheritdoc />
        public IEnumerable<Nonterminal> NonTerminalSymbols => _nonterminalToIndex.Keys;

        /// <inheritdoc />
        public IEnumerable<Nonterminal> TrimmedNonTerminalSymbols => _nonterminalToIndex.Keys.Where(symbol => !StartSymbol.Equals(symbol));

        /// <inheritdoc />
        public LrAction Action(int state, Terminal<TTokenKind> token)
        {
            return _actionTable[state, _terminalToIndex[token]];
        }

        /// <inheritdoc />
        public int Goto(int state, Nonterminal variable)
        {
            int symbolIndex = _nonterminalToIndex[variable];
            return symbolIndex == 0 ? 0 : _gotoTable[state, symbolIndex - 1];
        }

        /// <inheritdoc />
        public bool AnyConflicts => _conflictTable.Count > 0;

        /// <inheritdoc />
        public IEnumerable<LrConflict<TTokenKind>> Conflicts => _conflictTable.Values;
    }

    // TODO: Use this on the ParsingTable/Parser class
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum LrParserKind
    {
        /// <summary>
        /// LR(0) parsing table given rise to an LR(0) parser (if the
        /// parser table have no conflicts the grammar is LR(0))
        /// </summary>
        LR0,
        /// <summary>
        /// SLR(1) parsing table given rise to an SLR(1) parser (if the
        /// parser table have no conflicts the grammar is SLR(1))
        /// </summary>
        SLR1
    }
}
