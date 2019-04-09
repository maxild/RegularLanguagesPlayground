using System;
using System.Collections.Generic;
using System.Linq;
using AutomataLib;

namespace AutomataRepresentations
{
    /// <summary>
    /// Adjacency (Q x Σ)-matrix used as a transition table.
    /// This is the method of choice for a complete deterministic automaton on a small alphabet
    /// and when letters can be assimilated to indices on an array (US-ASCII). This kind of representation
    /// implicitly assumes that the working alphabet is fixed and known by advance. This is not a good
    /// representation for experimenting with simple DFA models, but an excellent model for a Lexer on
    /// the US-ASCII alphabet.
    ///
    /// Advantages:
    ///    - Simple model that mimics the delta-function in RAM.
    ///    - Best performance when recognizing input (linear in the input length O(n)).
    ///    - Perfect match for complete DFA.
    /// Disadvantages
    ///    - Input alphabet must be indexable (as in a range)
    ///    - Input alphabet need to be determined in advance (it must be fixed).
    ///    - Big alphabet (Unicode) requires a lot of RAM.
    /// </summary>
    public class DfaAdjacencyMatrix<TState> : IDeterministicFiniteAutomaton<char, int>, IFiniteAutomatonStateHomomorphism<int>
        where TState : IEquatable<TState>
    {
        private readonly TState[] _stateLabel; // one-way translation should be sufficient

        // NOTE: We require that input alphabet is a (sub)range of US-ASCII codes (127)
        // internal state machine based on int transitions (int source, int label, int target)
        // transformed to efficient 2DArray (table based transitions)
        private readonly int _maxState;
        private readonly char _minAscii;
        private readonly char _maxAscii;

        // A non-deterministic finite-state automaton (NFA) A = (Q, Σ, δ, q0, F) consists of a finite set of
        // states Q = {q1, ..., qn}, a finite set of input symbols or input alphabet Σ = {a1, ..., ak},
        // a set of transitions δ = {τ1, ..., τm} ⊆ Q × Σ × Q, the initial state q0 and the subset of accepting
        // states F ⊆ Q. For every (p, a) ∈ Q × Σ, the mapping δ(p, a) represents a subset of states that the
        // machine can transition into from state p on input a
        //                           δ(p, a) = {q ∈ Q : (p, a, q) ∈ δ}.
        // The number of states in the NFA is |Q| = n, the size of the alphabet is |Σ| = k and the number of
        // defined transitions is |δ| = m.
        //
        // A non-deterministic finite-state automaton (DFA) can be regarded as particular case of a NFA where
        // all sets δ(p, a) contain a single state, and therefore m = kn (i.e. a matrix).
        //      DFA is trimmed if |δ(p, a)| ≤ 1 for all (p, a) ∈ Q × Σ      (sparse matrix)
        //      DFA is complete if |δ(p, a)| = 1 for all (p, a) ∈ Q × Σ     (matrix)
        // We often trim a DFA because of a canonical error state. In a trimmed DFA we allow some transitions
        // to be undefined in the DFA. In A 2D-table driven design, it is only possible to define a complete DFA,
        // and make default(int) = 0 (zero) be the error state. When showing the digraph of the DFA (the transition
        // diagram) the error state should not be visible.
        private readonly int[,] _nextState;
        private readonly HashSet<int> _acceptStates;

        public DfaAdjacencyMatrix(
            IEnumerable<TState> states, // should be unique...we do not test this here
            IEnumerable<char> alphabet, // should be unique...we do not test this here
            IEnumerable<Transition<char, TState>> transitions,
            TState startState,
            IEnumerable<TState> acceptStates)
        {
            _stateLabel = states.ToArray();
            _maxState = _stateLabel.Length - 1;

            char minAscii = char.MaxValue, maxAscii = char.MinValue;
            foreach (char c in alphabet)
            {
                if (c < minAscii) minAscii = c;
                if (c > maxAscii) maxAscii = c;
            }

            if (minAscii == char.MinValue)
            {
                throw new ArgumentException("Empty alphabet is not supported.");
            }

            // TODO: This can be removed, or made configurable
            if (_maxAscii > 127)
            {
                throw new ArgumentException("Only US-ASCII is supported.");
            }

            int alphabetSize = maxAscii - minAscii + 1;

            _minAscii = minAscii;
            _maxAscii = maxAscii;
            _nextState = new int[_stateLabel.Length, alphabetSize];

            StartState = Array.IndexOf(_stateLabel, startState);
            if (StartState < 0)
            {
                throw new ArgumentException($"The start state '{startState}' is not contained in the set of states.");
            }

            _acceptStates = new HashSet<int>();
            foreach (TState acceptingState in acceptStates)
            {
                int accept = Array.IndexOf(_stateLabel, acceptingState);
                if (accept < 0)
                {
                    throw new ArgumentException($"The accept state '{acceptingState}' is not contained in the set of states.");
                }
                _acceptStates.Add(accept);
            }

            var hash = new Dictionary<TState, int>();
            for (int i = 0; i < _stateLabel.Length; i++)
            {
                hash.Add(_stateLabel[i], i);
            }

            if (hash.Count != _stateLabel.Length)
            {
                throw new ArgumentException("States must have unique names");
            }

            foreach (var move in transitions)
            {
                int source = hash[move.SourceState];
                int target = hash[move.TargetState];
                _nextState[source, move.Label - _minAscii] = target;
            }

            for (char c = _minAscii; c <= _maxAscii; c++)
            {
                if (NextState(0, c) != 0)
                    throw new ArgumentException("State 0 must be a sink state");
            }

            if (_acceptStates.Contains(0))
                throw new ArgumentException("State 0 cannot be an accept state.");
        }

        public int AlphabetSize => _maxAscii - _minAscii + 1;

        public int StateSize => _maxState + 1;

        public int TableSize => StateSize * AlphabetSize;

        public int GetTrimmedTableSize()
        {
            int size = 0;
            for (int s = 0; s < _nextState.GetLength(0); s += 1)
            {
                for (int c = 0; c < _nextState.GetLength(1); c += 1)
                {
                    int next = _nextState[s, c];
                    if (next != 0)
                    {
                        size += 1;
                    }
                }
            }
            return size;
        }

        int NextState(int s, char c)
        {
            return _nextState[s, c - _minAscii];
        }

        public int TransitionFunction(int state, IEnumerable<char> input)
        {
            int s = state;
            foreach (var c in input)
            {
                s = NextState(s, c);
            }
            return s;
        }

        public bool IsMatch(string input)
        {
            return IsAcceptState(TransitionFunction(StartState, Letterizer<char>.Default.GetLetters(input)));
        }

        public string PrintTable()
        {
            // TODO: make TransitionTable type, because then Format logic is not duplicated
            return null;
        }

        public bool IsAcceptState(int state)
        {
            return _acceptStates.Contains(state);
        }

        public IEnumerable<int> GetStates()
        {
            return Enumerable.Range(0, _maxState);
        }

        public IEnumerable<int> GetTrimmedStates()
        {
            // We do not show the error state, because state graph must be a trimmed DFA
            return Enumerable.Range(1, _maxState - 1);
        }

        public string GetStateLabel(int state)
        {
            return _stateLabel[state].ToString();
        }

        public IEnumerable<char> GetAlphabet()
        {
            for (char c = _minAscii; c <= _maxAscii; c++)
            {
                yield return c;
            }
        }

        public IEnumerable<char> GetNullableAlphabet()
        {
            throw new NotImplementedException();
        }

        public int StartState { get; }

        public IEnumerable<int> GetAcceptStates()
        {
            return _acceptStates;
        }

        // Complete DFA
        public IEnumerable<Transition<char, int>> GetTransitions()
        {
            for (int s = 0; s < _nextState.GetLength(0); s += 1)
            {
                for (int c = 0; c < _nextState.GetLength(1); c += 1)
                {
                    int next = _nextState[s, c];
                    yield return Transition.Move(s, (char)(c + _minAscii), next);
                }
            }
        }

        // Trimmed DFA
        public IEnumerable<Transition<char, int>> GetTrimmedTransitions()
        {
            // we do not show the error state!!!
            for (int s = 1; s < _nextState.GetLength(0); s += 1)
            {
                for (int c = 0; c < _nextState.GetLength(1); c += 1)
                {
                    int next = _nextState[s, c];
                    if (next != 0) // exclude error state
                    {
                        yield return Transition.Move(s, (char)(c + _minAscii), next);
                    }
                }
            }
        }
    }
}
