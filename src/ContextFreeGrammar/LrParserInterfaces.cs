using System;
using System.Collections.Generic;
using AutomataLib;

namespace ContextFreeGrammar
{
    public interface IProductionsContainer
    {
        /// <summary>
        /// Productions are numbered 0,1,2,...,N
        /// </summary>
        IReadOnlyList<Production> Productions { get; }
    }

    public interface IShiftReduceParsingTable<TTokenKind> : IProductionsContainer
        where TTokenKind : struct, Enum
    {
        /// <summary>
        /// The value of the start state (that is always zero for any shift-reduce parser)
        /// </summary>
        int StartState { get; }

        /// <summary>
        /// Get the action to be performed by the shift-reduce parser.
        /// </summary>
        /// <param name="state">The current state</param>
        /// <param name="token">The current input token (i.e. terminal symbol).</param>
        /// <returns>The action to be performed by the shift-reduce parser (shift, reduce, accept or error)</returns>
        LrAction Action(int state, Terminal<TTokenKind> token);

        /// <summary>
        /// Get the new state to push to the stack after the top of the stack handle have been reduced.
        /// </summary>
        /// <param name="state">
        /// The state of top of the stack after popping a handle off the stack (i.e. the state of the LR(k) automaton just
        /// before entering the state of the beginning of the recognized handle substring).
        /// </param>
        /// <param name="variable">
        /// The variable (A) on top of the stack after a reduce (A → β) to that variable.
        /// </param>
        /// <returns>The new state to push to the stack.</returns>
        int Goto(int state, Nonterminal variable);

        /// <summary>
        /// Get the LR(k) item set (of the canonical LR(k) collection) behind some integer state.
        /// </summary>
        /// <param name="state">The integer state.</param>
        /// <returns>The LR(k) item set (aka items).</returns>
        ProductionItemSet<TTokenKind> GetItems(int state);

        /// <summary>
        /// If the grammar is ambiguous, then we have found some conflicts in the parsing table.
        /// </summary>
        bool AnyConflicts { get; }

        /// <summary>
        /// The sequence of found conflicts.
        /// </summary>
        IEnumerable<LrConflict<TTokenKind>> Conflicts { get; }
    }

    public interface IShiftReduceParser<TTokenKind> : IShiftReduceParsingTable<TTokenKind>
        where TTokenKind : struct, Enum
    {
        /// <summary>
        /// The name of the LHS variable in the augmented production rule.
        /// </summary>
        Nonterminal StartSymbol { get; }

        /// <summary>
        /// Terminal symbols, including the EOF marker symbol ($).
        /// </summary>
        IEnumerable<Terminal<TTokenKind>> TerminalSymbols { get; }

        /// <summary>
        /// Nonterminal symbols, including any augmented start symbol (e.g. S').
        /// </summary>
        IEnumerable<Nonterminal> NonTerminalSymbols { get; }

        /// <summary>
        /// Nonterminal symbols, excluding any augmented start symbol (e.g. S').
        /// </summary>
        IEnumerable<Nonterminal> TrimmedNonTerminalSymbols { get; }

        /// <summary>
        /// Get the sequence of states 0,1,2,...,maxState
        /// </summary>
        /// <returns></returns>
        IEnumerable<int> GetStates(); // TODO: States should be equivalent to DFA states
    }
}
