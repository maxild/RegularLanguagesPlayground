using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using AutomataLib;

namespace ContextFreeGrammar
{
    /// <summary>
    /// Non-deterministic Finite Automaton (Q, Σ, delta, q(0), F) from Automata Theory (with
    /// possible ε-transitions, aka ε-moves). Used in step 1 of LR(0) Automaton construction.
    /// </summary>
    /// <typeparam name="TState"></typeparam>
    /// <typeparam name="TAlphabet"></typeparam>
    public class Nfa<TState, TAlphabet> : IFiniteAutomaton<TAlphabet, TState>
        where TAlphabet : IEquatable<TAlphabet>
        where TState : IEquatable<TState>
    {
        private static readonly ISet<TState> s_deadState = new HashSet<TState>();

        // Adjacency ('sparse') list representation of digraph with labeled edges (moves, transitions)
        //
        //      _delta: array/list of adjacency list of moves/transitions
        //
        // We can only use (outer)
        //
        //private readonly Dictionary<TState, List<Move<TAlphabet>>> _delta;                // Automaton<TAlphabet> uses this one
        //private readonly Dictionary<TState, List<Transition<TAlphabet>>> _delta;          // Nfa<TAlphabet> uses this one
        private readonly Dictionary<TState, IDictionary<TAlphabet, ISet<TState>>> _delta;

        private readonly HashSet<TState> _acceptStates;
        private readonly HashSet<TState> _states;

        // used to to build DFA in single call
        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        public Nfa(
            IEnumerable<Transition<TAlphabet, TState>> transitions,
            TState startState,
            IEnumerable<TState> acceptStates)
        {
            StartState = startState;
            _acceptStates = new HashSet<TState>(acceptStates);
            _states = new HashSet<TState>(acceptStates) { startState };
            _delta = new Dictionary<SourceTransitionPair<TState, TAlphabet>, List<TState>>();

            foreach (var triple in transitions)
            {
                _states.Add(triple.SourceState);
                _states.Add(triple.TargetState);
                var pair = Transition.FromPair(triple.SourceState, triple.Label);
                if (_delta.ContainsKey(pair))
                    _delta[pair].Add(triple.TargetState);
                else
                    _delta[pair] = new List<TState> {triple.TargetState};
            }
        }

        public TState StartState { get; }

        public bool IsAcceptState(TState state)
        {
            return _acceptStates.Contains(state);
        }

        public IEnumerable<TState> GetStates()
        {
            return _states;
        }

        public IEnumerable<TState> GetTrimmedStates()
        {
            return GetStates();
        }

        public IEnumerable<TState> GetAcceptStates()
        {
            return _acceptStates;
        }

        public IEnumerable<Transition<TAlphabet, TState>> GetTransitions()
        {
            foreach (KeyValuePair<SourceTransitionPair<TState, TAlphabet>, List<TState>> kvp in _delta)
            {
                TState sourceState = kvp.Key.SourceState;
                TAlphabet label = kvp.Key.Label;
                foreach (TState targetState in kvp.Value)
                {
                    yield return Transition.Move(sourceState, label, targetState);
                }
            }
        }

        public IEnumerable<Transition<TAlphabet, TState>> GetTrimmedTransitions()
        {
            return GetTransitions();
        }

        //public DeterministicAutomata<TAlphabet> SubsetConstruction()
        //{
        //    // int vs ProductionItemSet...renaming of states, and ToDotLanguage with state descriptions
        //    return null;
        //}
    }
}
