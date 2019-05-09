using System;
using System.Collections.Generic;
using AutomataLib;

namespace ContextFreeGrammar
{
    public interface IProductionsContainer<TNonterminalSymbol> where TNonterminalSymbol : Symbol
    {
        /// <summary>
        /// Productions are numbered 0,1,2,...,N
        /// </summary>
        IReadOnlyList<Production<TNonterminalSymbol>> Productions { get; }
    }

    public interface IShiftReduceParsingTable<TNonterminalSymbol, TTerminalSymbol> : IProductionsContainer<TNonterminalSymbol>
        where TNonterminalSymbol : Symbol, IEquatable<TNonterminalSymbol>
        where TTerminalSymbol : Symbol, IEquatable<TTerminalSymbol>
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
        LrAction Action(int state, TTerminalSymbol token);

        /// <summary>
        /// Get the new state to push to the stack after the top of the stack handle have been reduced.
        /// </summary>
        /// <param name="state">
        /// The state of top of the stack after popping a handle off the stack (i.e. the state of the LR(0) automaton just
        /// before entering the state of the beginning of the recognized handle substring).
        /// </param>
        /// <param name="variable">
        /// The variable (A) on top of the stack after a reduce (A → β) to that variable.
        /// </param>
        /// <returns>The new state to push to the stack.</returns>
        int Goto(int state, TNonterminalSymbol variable);

        /// <summary>
        /// Get the LR(0) item set (of the canonical LR(0) collection) behind some integer state.
        /// </summary>
        /// <param name="state">The integer state.</param>
        /// <returns>The LR(0) item set (aka items).</returns>
        ProductionItemSet<TNonterminalSymbol> GetItems(int state);

        /// <summary>
        /// If the grammar is ambiguous, then we have found some conflicts in the parsing table.
        /// </summary>
        bool AnyConflicts { get; }

        /// <summary>
        /// The sequence of found conflicts.
        /// </summary>
        IEnumerable<LrConflict<TTerminalSymbol>> Conflicts { get; }
    }

    public interface IShiftReduceParser<TNonterminalSymbol, TTerminalSymbol> : IShiftReduceParsingTable<TNonterminalSymbol, TTerminalSymbol>
        where TNonterminalSymbol : Symbol, IEquatable<TNonterminalSymbol>
        where TTerminalSymbol : Symbol, IEquatable<TTerminalSymbol>
    {
        /// <summary>
        /// The name of the LHS variable in the augmented production rule.
        /// </summary>
        TNonterminalSymbol StartSymbol { get; }

        /// <summary>
        /// Terminal symbols, including the EOF marker symbol ($).
        /// </summary>
        IEnumerable<TTerminalSymbol> TerminalSymbols { get; }

        /// <summary>
        /// Nonterminal symbols, including any augmented start symbol (e.g. S').
        /// </summary>
        IEnumerable<TNonterminalSymbol> NonTerminalSymbols { get; }

        /// <summary>
        /// Nonterminal symbols, excluding any augmented start symbol (e.g. S').
        /// </summary>
        IEnumerable<TNonterminalSymbol> TrimmedNonTerminalSymbols { get; }

        /// <summary>
        /// Get the sequence of states 0,1,2,...,maxState
        /// </summary>
        /// <returns></returns>
        IEnumerable<int> GetStates();
    }
}