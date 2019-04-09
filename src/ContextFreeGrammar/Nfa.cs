using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using AutomataLib;

namespace ContextFreeGrammar
{
    /// <summary>
    /// Non-deterministic Finite Automaton (Q, Σ, delta, q(0), F) from Automata Theory (with
    /// possible ε-transitions, aka ε-moves). Used in step 1 of LR(0) Automaton construction.
    /// </summary>
    /// <typeparam name="TState"></typeparam>
    /// <typeparam name="TAlphabet"></typeparam>
    public class Nfa<TState, TAlphabet> : INondeterministicFiniteAutomaton<TAlphabet, TState>
        where TAlphabet : IEquatable<TAlphabet>
        where TState : IEquatable<TState>
    {
        //private static readonly ISet<TState> s_deadState = new HashSet<TState>();

        private readonly Dictionary<SourceTransitionPair<TState, TAlphabet>, List<TState>> _delta;

        private readonly HashSet<TState> _acceptStates;
        private readonly HashSet<TState> _states;
        private readonly HashSet<TAlphabet> _alphabet;

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
            _alphabet = new HashSet<TAlphabet>();

            foreach (var triple in transitions)
            {
                _states.Add(triple.SourceState);
                _states.Add(triple.TargetState);
                var pair = Transition.FromPair(triple.SourceState, triple.Label);
                if (_delta.ContainsKey(pair))
                    _delta[pair].Add(triple.TargetState);
                else
                    _delta[pair] = new List<TState> {triple.TargetState};
                if (triple.Label.Equals(Transition.Epsilon<TAlphabet>()))
                    IsEpsilonNfa = true;
                else
                    _alphabet.Add(triple.Label); // alphabet does not contain epsilon
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

        public IEnumerable<TAlphabet> GetAlphabet()
        {
            return _alphabet;
        }

        // TODO: Maybe remove this
        public IEnumerable<TAlphabet> GetNullableAlphabet()
        {
            return _alphabet.Concat(new []{Transition.Epsilon<TAlphabet>()});
        }

        public bool IsEpsilonNfa { get; }

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

        public Dfa<Set<TState>, TAlphabet> ToDfa()
        {
            var newStartState = EpsilonClose(new Set<TState> {StartState});
            var newAcceptStates = new HashSet<Set<TState>>();
            var newTransitions = new List<Transition<TAlphabet, Set<TState>>>();

            var newStates = new HashSet<Set<TState>> {newStartState};

            // Lazy form of Subset Construction where only reachable nodes
            // are added to the following work list of marked subsets
            var markedVisitedStates = new Queue<Set<TState>>(); // work list that preserves insertion order
            markedVisitedStates.Enqueue(newStartState);

            while (markedVisitedStates.Count != 0)
            {
                // subset S
                Set<TState> subsetSourceState = markedVisitedStates.Dequeue();

                if (subsetSourceState.Overlaps(GetAcceptStates()))
                {
                    newAcceptStates.Add(subsetSourceState);
                }

                // For all non-epsilon labels
                foreach (var label in GetAlphabet())
                {
                    // subset T
                    var subsetTargetState = new Set<TState>();

                    // For all s in S, add all non-epsilon transitions (s, label) -> t to T
                    foreach (TState s in subsetSourceState)
                    {
                        subsetTargetState.AddRange(Delta(Transition.FromPair(s, label)));
                    }

                    // Epsilon-close all T such that (S, label) -> T
                    subsetTargetState = EpsilonClose(subsetTargetState);

                    if (!newStates.Contains(subsetTargetState))
                    {
                        newStates.Add(subsetTargetState);
                        markedVisitedStates.Enqueue(subsetTargetState);
                    }

                    // Add (S, label) -> T transition
                    newTransitions.Add(Transition.Move(subsetSourceState, label, subsetTargetState));
                }
            }

            return new Dfa<Set<TState>, TAlphabet>(newStates, GetAlphabet(), newTransitions, newStartState, newAcceptStates);
        }

        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        Set<TState> EpsilonClose(IEnumerable<TState> states)
        {
            var markedVisitedStates = new Queue<TState>(states);
            // base step
            var closure = new Set<TState>(states);
            // induction step: recurse until no more states in a round
            while (markedVisitedStates.Count != 0)
            {
                TState sourceState = markedVisitedStates.Dequeue();
                // if any epsilon-moves add them all to the closure (union)
                foreach (var targetState in Delta(Transition.FromEpsilonPair<TState, TAlphabet>(sourceState)))
                {
                    if (!closure.Contains(targetState))
                    {
                        closure.Add(targetState);
                        markedVisitedStates.Enqueue(targetState);
                    }
                }
            }
            return closure;
        }

        IEnumerable<TState> Delta(SourceTransitionPair<TState, TAlphabet> pair)
        {
            return _delta.TryGetValue(pair, out var targetStates) ? targetStates : Enumerable.Empty<TState>();
        }
    }
}
