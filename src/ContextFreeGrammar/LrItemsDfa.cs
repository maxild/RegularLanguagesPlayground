using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using AutomataLib;

namespace ContextFreeGrammar
{
    // TState = ProductionItemSet<TTokenKind>
    // TAlphabet = Symbol

    /// <summary>
    /// Deterministic Finite Automaton (Q, Σ, delta, q(0), F) from Automata Theory.
    /// </summary>
    public class LrItemsDfa<TTokenKind> : IDeterministicFiniteAutomaton<Symbol, int>, IFiniteAutomatonStateHomomorphism<int>
        where TTokenKind : Enum
    {
        private readonly ProductionItemSet<TTokenKind>[] _originalStates; // one-way translation should be sufficient to show descriptive labels
        private readonly Dictionary<Symbol, int> _alphabetToIndex;
        private readonly Symbol[] _indexToAlphabet;

        private readonly int[,] _nextState; // adjacency matrix with dead state at index zero
        private readonly HashSet<int> _acceptStates;

        public LrItemsDfa(
            IEnumerable<ProductionItemSet<TTokenKind>> states,
            IEnumerable<Symbol> alphabet,
            IEnumerable<Transition<Symbol, ProductionItemSet<TTokenKind>>> transitions,
            ProductionItemSet<TTokenKind> startState,
            IEnumerable<ProductionItemSet<TTokenKind>> acceptStates)
        {
            _originalStates = states.ToArray();
            MaxState = _originalStates.Length; // 0,1,2,...,maxState, where dead state is at index zero

            // renaming all states to integers
            var indexMap = new Dictionary<ProductionItemSet<TTokenKind>, int>(MaxState); // dead state excluded here
            int stateIndex = 1;
            foreach (ProductionItemSet<TTokenKind> state in _originalStates)
            {
                indexMap.Add(state, stateIndex);
                stateIndex += 1;
            }

            _indexToAlphabet = alphabet.ToArray();
            _alphabetToIndex = new Dictionary<Symbol, int>();
            for (int i = 0; i < _indexToAlphabet.Length; i++)
            {
                _alphabetToIndex[_indexToAlphabet[i]] = i;
            }

            StartState = indexMap[startState];

            _acceptStates = new HashSet<int>();
            foreach (ProductionItemSet<TTokenKind> state in acceptStates)
            {
                _acceptStates.Add(indexMap[state]);
            }

            _nextState = new int[MaxState + 1, _alphabetToIndex.Count];

            foreach (var move in transitions)
            {
                int source = indexMap[move.SourceState];
                int target = indexMap[move.TargetState];
                _nextState[source, _alphabetToIndex[move.Label]] = target;
            }
        }

        public int StartState { get; }

        public int MaxState { get; }

        public bool IsAcceptState(int state)
        {
            return _acceptStates.Contains(state);
        }

        /// <summary>
        /// States 0,...,MaxState (inclusive the dead state = 0)
        /// </summary>
        public IEnumerable<int> GetStates()
        {
            return Enumerable.Range(0, MaxState + 1);  // 0, 1, 2,..., maxState
        }

        /// <summary>
        /// States 1,...,MaxState (exclusive the dead state = 0)
        /// </summary>
        public IEnumerable<int> GetTrimmedStates()
        {
            // We do not show the dead state
            return Enumerable.Range(1, MaxState);      // 1, 2,..., maxState
        }

        public IEnumerable<Symbol> GetAlphabet()
        {
            return _alphabetToIndex.Keys;
        }

        public IEnumerable<int> GetAcceptStates()
        {
            return _acceptStates;
        }

        public IEnumerable<Transition<Symbol, int>> GetTransitions()
        {
            for (int s = 0; s < _nextState.GetLength(0); s += 1)
            {
                for (int c = 0; c < _nextState.GetLength(1); c += 1)
                {
                    int next = _nextState[s, c];
                    yield return Transition.Move(s, _indexToAlphabet[c], next);
                }
            }
        }

        public IEnumerable<Transition<Symbol, int>> GetTrimmedTransitions()
        {
            // exclude dead (error) state
            for (int s = 1; s < _nextState.GetLength(0); s += 1)
            {
                for (int c = 0; c < _nextState.GetLength(1); c += 1)
                {
                    int next = _nextState[s, c];
                    if (next != 0) // exclude error state
                    {
                        yield return Transition.Move(s, _indexToAlphabet[c], next);
                    }
                }
            }
        }

        /// <summary>
        /// Get the set of possible predecessor states from where some label accesses the given state.
        /// </summary>
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public IReadOnlySet<int> PRED(int state, Symbol label)
        {
            // Highly inefficient
            var predStates = new Set<int>();
            int j = _alphabetToIndex[label];
            for (int i = 1; i <= MaxState; i += 1)
                if (_nextState[i, j] == state)
                    predStates.Add(i);
            return predStates;
        }

        /// <summary>
        /// Get the set of possible predecessor states from where some label accesses the given states.
        /// </summary>
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public IReadOnlySet<int> PRED(IEnumerable<int> states, Symbol label)
        {
            var predecessorStates = new Set<int>();
            foreach (int state in states)
            {
                predecessorStates.UnionWith(PRED(state, label));
            }
            return predecessorStates;
        }

        /// <summary>
        /// Get the set of possible predecessor states from where some (reversed) input accesses the given state.
        /// </summary>
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public IReadOnlySet<int> PRED(int state, IEnumerable<Symbol> reversedInput)
        {
            IReadOnlySet<int> states = new Set<int>(state.AsSingletonEnumerable());
            foreach (var c in reversedInput)
            {
                states = PRED(states, c);
            }
            return states;
        }

        public int TransitionFunction((int, Symbol) pair)
        {
            return _nextState[pair.Item1, _alphabetToIndex[pair.Item2]];
        }

        public int TransitionFunction(int s, Symbol label)
        {
            return _nextState[s, _alphabetToIndex[label]];
        }

        public int TransitionFunction(int state, IEnumerable<Symbol> input)
        {
            int s = state;
            foreach (var c in input)
            {
                s = TransitionFunction(s, c);
            }
            return s;
        }

        public bool IsMatch(IEnumerable<Symbol> input)
        {
            return IsAcceptState(TransitionFunction(StartState, input));
        }

        public ProductionItemSet<TTokenKind> GetUnderlyingState(int state)
        {
            int originalIndex = state - 1; // dead state occupies index zero in matrix, but not in _originalStates array
            return _originalStates[originalIndex];
        }

        /// <summary>
        /// Get the index state of some underlying state defined by a predicate.
        /// </summary>
        public int IndexOfUnderlyingState(Func<ProductionItemSet<TTokenKind>, bool> predicate)
        {
            // Linear O(n) search is the only option, but augmented kernel/reduce item should be contained in state 2
            int state = -1;
            for (int originalIndex = 0; originalIndex < _originalStates.Length; originalIndex += 1)
                if (predicate(_originalStates[originalIndex]))
                    state = originalIndex + 1; // dead state occupies index zero in matrix, but not in _originalStates array
            return state;
        }

        public string GetStateLabel(int state, string sep)
        {
            int originalIndex = state - 1; // dead state occupies index zero in matrix, but not in _originalStates array

            // HACK: we special case two type of canonical LR(0) item sets to make graphviz images prettier
            if (_originalStates[originalIndex] is ProductionItemSet<TTokenKind> itemSet)
            {
                // LR(0) items separated by '\l', and kernel and closure items are separated by a newline
                return itemSet.ClosureItems.Any()
                ? string.Join(sep, itemSet.KernelItems) + "\\n" + sep + string.Join(sep, itemSet.ClosureItems) + sep
                : string.Join(sep, itemSet.KernelItems) + sep;
            }
            if (_originalStates[originalIndex] is AutomataLib.ISet<ProductionItem<TTokenKind>> itemSet2)
            {
                // LR(0) items separated by '\l' ('\l' in dot language makes the preceding text left aligned in Graphviz tool)
                return string.Join(sep, itemSet2) + sep;
            }

            return _originalStates[originalIndex].ToString();
        }
    }

    //public static class DfaLr0Extensions
    //{
    //    /// <summary>
    //    /// Find all LR(k) item sets (i.e. underlying states) with a given CORE LR(0) item.
    //    /// </summary>
    //    public static IEnumerable<ProductionItemSet<TNonterminalSymbol, TTerminalSymbol>> URCORE<TNonterminalSymbol, TTerminalSymbol>(
    //        this Dfa<ProductionItemSet<TNonterminalSymbol, TTerminalSymbol>, Symbol> dfaLr0,
    //        MarkedProduction<TNonterminalSymbol> dottedProduction)
    //            where TNonterminalSymbol : Symbol, IEquatable<TNonterminalSymbol>
    //            where TTerminalSymbol : Symbol, IEquatable<TTerminalSymbol>
    //    {
    //        // TODO: Finder bare den foerste!!!!
    //        int state = dfaLr0.IndexOfUnderlyingState(itemSet => itemSet.CoreOfKernelContains(dottedProduction));

    //    }

    //    /// <summary>
    //    /// Find all integer states with a given CORE LR(0) item.
    //    /// </summary>
    //    public static IEnumerable<int> URCORE2<TNonterminalSymbol, TTerminalSymbol>(
    //        this Dfa<ProductionItemSet<TNonterminalSymbol, TTerminalSymbol>, Symbol> dfaLr0, MarkedProduction<TNonterminalSymbol> dottedProduction)
    //        where TNonterminalSymbol : Symbol, IEquatable<TNonterminalSymbol>
    //        where TTerminalSymbol : Symbol, IEquatable<TTerminalSymbol>
    //    {
    //        int state = dfaLr0.IndexOfUnderlyingState(itemSet => itemSet.CoreOfKernelContains(dottedProduction));

    //    }
    //}
}
