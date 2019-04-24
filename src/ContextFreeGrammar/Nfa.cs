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
        private readonly SortedSet<TAlphabet> _alphabet; // ASCII sort order

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
            _alphabet = new SortedSet<TAlphabet>();

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

        /// <summary>
        /// Create Deterministic Finite Automaton by lazy form of subset construction (Rabin and Scott, )
        /// </summary>
        /// <returns>Deterministic Finite Automaton created by lazy form of subset construction</returns>
        public Dfa<Set<TState>, TAlphabet> ToDfa()
        {
            // The subset construction is an example of a fixed-point computation, where an application of a
            // monotone function to some collection of sets drawn from a domain whose structure is known is
            // performed iteratively. The computation will terminate when an iteration step produces a state
            // where further iteration produces the same answer — a “fixed point” in the space of successive
            // iterates.
            //
            // For the subset construction, the domain is the power set of all possible subsets of the NFA states.
            // The while loop adds elements (subsets) to Q; it cannot remove an element from Q. We can view
            // the while loop as a monotone increasing function f, which means that for a set x, f(x) ≥ x. (The
            // comparison operator ≥ is ⊇.) Since Q can have at most 2^N distinct elements, the while loop can iterate
            // at most 2^N times. It may, of course, reach a fixed point and halt more quickly than that.

            var newStartState = EpsilonClose(new Set<TState> {StartState});

            var newAcceptStates = new HashSet<Set<TState>>();
            var newTransitions = new List<Transition<TAlphabet, Set<TState>>>();

            var newStates = new InsertionOrderedSet<Set<TState>> {newStartState};

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

                    // Core Items: For all s in S, add all non-epsilon transitions (s, label) -> t to T
                    foreach (TState s in subsetSourceState)
                    {
                        subsetTargetState.AddRange(Delta(Transition.FromPair(s, label)));
                    }

                    // Ignore empty subset (implicit transition to dead state in this case)
                    if (subsetTargetState.Count == 0) continue;

                    // Closure Items: Epsilon-close all T such that (S, label) -> T
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
