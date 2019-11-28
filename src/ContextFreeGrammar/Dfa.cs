using System;
using System.Collections.Generic;
using System.Linq;
using AutomataLib;

namespace ContextFreeGrammar
{
    /// <summary>
    /// Deterministic Finite Automaton (Q, Σ, delta, q(0), F) from Automata Theory.
    /// </summary>
    public class Dfa<TState, TAlphabet> : IDeterministicFiniteAutomaton<TAlphabet, int>, IFiniteAutomatonStateHomomorphism<int>
    {
        private readonly TState[] _originalStates; // one-way translation should be sufficient to show descriptive labels
        private readonly Dictionary<TAlphabet, int> _alphabetToIndex;
        private readonly TAlphabet[] _indexToAlphabet;

        private readonly int[,] _nextState;
        private readonly HashSet<int> _acceptStates;

        public Dfa(
            IEnumerable<TState> states,
            IEnumerable<TAlphabet> alphabet,
            IEnumerable<Transition<TAlphabet, TState>> transitions,
            TState startState,
            IEnumerable<TState> acceptStates)
        {
            _originalStates = states.ToArray();
            MaxState = _originalStates.Length; // 0,1,2,...,maxState, where dead state is at index zero

            // renaming all states to integers
            var indexMap = new Dictionary<TState, int>(MaxState); // dead state excluded here
            int stateIndex = 1;
            foreach (TState state in _originalStates)
            {
                indexMap.Add(state, stateIndex);
                stateIndex += 1;
            }

            _indexToAlphabet = alphabet.ToArray();
            _alphabetToIndex = new Dictionary<TAlphabet, int>();
            for (int i = 0; i < _indexToAlphabet.Length; i++)
            {
                _alphabetToIndex[_indexToAlphabet[i]] = i;
            }

            StartState = indexMap[startState];

            _acceptStates = new HashSet<int>();
            foreach (TState state in acceptStates)
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

        public IEnumerable<TAlphabet> GetAlphabet()
        {
            return _alphabetToIndex.Keys;
        }

        public IEnumerable<int> GetAcceptStates()
        {
            return _acceptStates;
        }

        public IEnumerable<Transition<TAlphabet, int>> GetTransitions()
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

        public IEnumerable<Transition<TAlphabet, int>> GetTrimmedTransitions()
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

        public int TransitionFunction((int, TAlphabet) pair)
        {
            return _nextState[pair.Item1, _alphabetToIndex[pair.Item2]];
        }

        public int TransitionFunction(int s, TAlphabet label)
        {
            return _nextState[s, _alphabetToIndex[label]];
        }

        public int TransitionFunction(int state, IEnumerable<TAlphabet> input)
        {
            int s = state;
            foreach (var c in input)
            {
                s = TransitionFunction(s, c);
            }
            return s;
        }

        public bool IsMatch(string input)
        {
            return IsAcceptState(TransitionFunction(StartState, Letterizer<TAlphabet>.Default.GetLetters(input)));
        }

        public TState GetUnderlyingState(int state)
        {
            int originalIndex = state - 1; // dead state occupies index zero in matrix, but not in _originalStates array
            return _originalStates[originalIndex];
        }

        /// <summary>
        /// Get the index state of some underlying state defined by a predicate.
        /// </summary>
        public int IndexOfUnderlyingState(Func<TState, bool> predicate)
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
            if (_originalStates[originalIndex] is ProductionItemSet<Nonterminal, Terminal> itemSet)
            {
                // LR(0) items separated by '\l', and core and closure items are separated by a newline
                return itemSet.ClosureItems.Any()
                ? string.Join(sep, itemSet.KernelItems) + "\\n" + sep + string.Join(sep, itemSet.ClosureItems) + sep
                : string.Join(sep, itemSet.KernelItems) + sep;
            }
            if (_originalStates[originalIndex] is AutomataLib.ISet<ProductionItem<Nonterminal, Terminal>> itemSet2)
            {
                // LR(0) items separated by '\l' ('\l' in dot language makes the preceding text left aligned in Graphviz tool)
                return string.Join(sep, itemSet2) + sep;
            }

            return _originalStates[originalIndex].ToString();
        }
    }
}
