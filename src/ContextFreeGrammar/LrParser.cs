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
    // off the stack and push A instead. a shift action means that the parser is waiting to see more of the input before
    // it decides that a match has been found; a reduce action means that a match has been found.
    //
    // There are two types of conflicts we might encounter: shift-reduce and reduce-reduce:
    //
    //  1. Shift-reduce conflict. This occurs when the parser is faced with a choice of a shift action and a reduce action.
    //     (Yacc’s default action in the case of a shift-reduce conflict is to choose the shift action.)
    //  2. Reduce-reduce conflict. This occurs when the parser is faced with a choice of two different productions
    //     that could be used for a reduce action. (Yacc’s default action in the case of a reduce-reduce conflict is
    //     to reduce using the production that comes first, textually, in the input grammar specification.)
    //
    // In order to figure out the reason for a conflict, we have to find out (1) which LR(0) items (state) have a conflicts;
    // and (2) the reason for the conflict.

    // LR(0) Grammar
    // =============
    // A grammar is LR(0) if the following two conditions hold for all LR(0) item sets (aka items):
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
    // A grammar is SLR(1) if the following two conditions hold for all LR(0) item sets (aka items):
    //
    //  1. For any (shift) item A → α•aβ in the set, with terminal 'a', there is no (reduce) item
    //     B –> w• in that set with 'a' in Follow(B). In the parsing tables, this translates to no
    //     shift/­reduce conflict on any state. This means the successor function for symbol 'a'
    //     from that set either shifts to a new state or reduces, but not both.
    //  2. For any two (reduce) items A –> α• and B –> β• in the set, the follow sets must
    //     be disjoint, e.g. Follow(A) ∩ Follow(B) is empty. This translates to no reduce/­reduce
    //     conflict on any state. If more than one non­terminal can be reduced from this set,
    //     it must be possible to uniquely determine which using only one token of lookahead.


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

        private readonly IReadOnlyOrderedSet<ProductionItemSet<TNonterminalSymbol>> _originalStates;

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

        public LrParser(
            Grammar<TNonterminalSymbol, TTerminalSymbol> grammar,
            IReadOnlyOrderedSet<ProductionItemSet<TNonterminalSymbol>> originalStates,
            IEnumerable<TNonterminalSymbol> nonterminalSymbols,
            IEnumerable<TTerminalSymbol> terminalSymbols,
            IEnumerable<LrActionEntry<TTerminalSymbol>> actionTableEntries,
            IEnumerable<LrGotoEntry<TNonterminalSymbol>> gotoTableEntries
            )
        {
            _grammar = grammar;
            _originalStates = originalStates;
            _maxState = originalStates.Count - 1;
            StartState = 0;

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

            // NOTE: Important that shift actions are inserted before reduce
            //       actions among the entries (conflict resolution)

            foreach (var entry in actionTableEntries)
            {
                var symbolIndex = _terminalToIndex[entry.TerminalSymbol];

                // error is the default action, and not an error will therefore indicate a conflict
                if (!_actionTable[entry.State, symbolIndex].IsError)
                {
                    // Only reduce actions can cause conflicts, when all shift actions are inserted first
                    Debug.Assert(entry.Action.IsReduce);

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
                        _conflictTable.Add((entry.State, entry.TerminalSymbol), new LrConflict<TTerminalSymbol>(
                            entry.State,
                            entry.TerminalSymbol,
                            new[]
                            {
                                _actionTable[entry.State, symbolIndex], // existing (shift or reduce) action
                                entry.Action                            // reduce action
                            }));
                    }

                    // choose the shift action (for any shift/reduce conflicts), or choose the reduce action
                    // with the lowest index (for any reduce/reduce conflicts), by ignoring to put the reduce action in
                    // the table (Yacc, Happy etc. all choose this strategy for resolving conflicts)
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

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public LrParser(
            Grammar<TNonterminalSymbol, TTerminalSymbol> grammar,
            IReadOnlyOrderedSet<ProductionItemSet<TNonterminalSymbol>> states,
            IEnumerable<TNonterminalSymbol> nonterminalSymbols,
            IEnumerable<TTerminalSymbol> terminalSymbols,
            IEnumerable<Transition<Symbol, ProductionItemSet<TNonterminalSymbol>>> transitions,
            ProductionItemSet<TNonterminalSymbol> startState
            )
        {
            _grammar = grammar;

            _originalStates = states;

            // no dead state, because we have explicit error action in ACTION table
            // and state 0 is the canonical S' → S production LR(0) item set that can
            // never be reached
            _maxState = _originalStates.Count - 1;

            // renaming all LR(0) item sets to integer states
            var indexMap = new Dictionary<ProductionItemSet<TNonterminalSymbol>, int>(capacity: _maxState);
            int stateIndex = 0;
            foreach (ProductionItemSet<TNonterminalSymbol> state in _originalStates)
            {
                indexMap.Add(state, stateIndex);
                stateIndex += 1;
            }

            // TODO: startState is really not needed
            StartState = indexMap[startState];
            if (StartState != 0)
                throw new InvalidOperationException("The initial start state should be the first item set in the sequence");

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

            //
            //
            // TODO: ComputeParsingTable method should be factored out of parsing table type
            // TODO: kan det goeres smartere uden foerst at danne transition triple array
            //
            //

            // NOTE: Important that shift actions are configured before reduce actions (conflict resolution)

            // Shift and Goto actions (directly from the transitions of the LR(0) automaton).
            // TODO: My guess is these are the same across all LR methods????!!!!????
            foreach (var move in transitions)
            {
                int source = indexMap[move.SourceState];
                int target = indexMap[move.TargetState];

                if (move.Label.IsTerminal)
                {
                    // If A → α•aβ is in LR(0) item set, where a is a terminal symbol
                    var a = (TTerminalSymbol) move.Label;
                    var symbolIndex = _terminalToIndex[a];
                    _actionTable[source, symbolIndex] = LrAction.Shift(target);
                }
                else
                {
                    // If A → α•Xβ is in LR(0) item set, where X is a nonterminal symbol
                    var X = (TNonterminalSymbol) move.Label;
                    int symbolIndex = _nonterminalToIndex[X];
                    _gotoTable[source, symbolIndex - 1] = target;
                }
            }

            // Reduce actions differ between different LR methods (SLR strategy uses FOLLOW(A) below)
            // TODO: My guess is these differ between different LR methods????!!!!????
            foreach (ProductionItemSet<TNonterminalSymbol> itemSet in _originalStates)
            {
                // If A → α• is in LR(0) item set, then set action[s, a] to 'reduce A → α•' (where A is not S')
                // for all a in T               (LR(0) table)
                // for all a in FOLLOW(A)       (SLR(1) table)
                if (itemSet.IsReduceAction)
                {
                    foreach (ProductionItem<TNonterminalSymbol> reduceItem in
                             itemSet.ReduceItems.OrderBy(item => item.ProductionIndex)) // choose reductions with lowest index
                    {
                        var state = indexMap[itemSet];

                        // LR(0) grammar rule
                        //foreach (var terminal in grammar.Terminals)
                        // SLR(1) grammar rule
                        foreach (var terminal in grammar.FOLLOW(reduceItem.Production.Head))
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

                // If S' → S• is in LR(0) item set, then set action[s, $] to accept
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

        /// <inheritdoc />
        public int StartState { get; }

        /// <inheritdoc />
        public TNonterminalSymbol StartSymbol => _grammar.Productions[StartState].Head;

        /// <inheritdoc />
        public IReadOnlyList<Production<TNonterminalSymbol>> Productions => _grammar.Productions;

        /// <inheritdoc />
        public ProductionItemSet<TNonterminalSymbol> GetItems(int state)
        {
            return _originalStates[state];
        }

        /// <inheritdoc />
        public IEnumerable<int> GetStates()
        {
            return Enumerable.Range(0, count: _maxState + 1);  // 0, 1, 2,...,maxState
        }

        /// <inheritdoc />
        public IEnumerable<TTerminalSymbol> TerminalSymbols => _terminalToIndex.Keys;

        /// <inheritdoc />
        public IEnumerable<TNonterminalSymbol> NonTerminalSymbols => _nonterminalToIndex.Keys;

        /// <inheritdoc />
        public IEnumerable<TNonterminalSymbol> TrimmedNonTerminalSymbols => _nonterminalToIndex.Keys.Where(symbol => !StartSymbol.Equals(symbol));

        /// <inheritdoc />
        public LrAction Action(int state, TTerminalSymbol token)
        {
            return _actionTable[state, _terminalToIndex[token]];
        }

        /// <inheritdoc />
        public int Goto(int state, TNonterminalSymbol variable)
        {
            int symbolIndex = _nonterminalToIndex[variable];
            return symbolIndex == 0 ? 0 : _gotoTable[state, symbolIndex - 1];
        }

        /// <inheritdoc />
        public bool AnyConflicts => _conflictTable.Count > 0;

        /// <inheritdoc />
        public IEnumerable<LrConflict<TTerminalSymbol>> Conflicts => _conflictTable.Values;
    }

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
