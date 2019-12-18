using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using AutomataLib;

namespace ContextFreeGrammar
{
    // TState = ProductionItem
    // TAlphabet = Symbol

    /// <summary>
    /// Non-deterministic Finite Automaton (Q, Σ, delta, q(0), F) from Automata Theory (with
    /// possible ε-transitions, aka ε-moves). Used in step 1 of LR(0) Automaton construction.
    /// </summary>
    public class LrItemNfa<TTokenKind> : INondeterministicFiniteAutomaton<Symbol, ProductionItem<TTokenKind>>
        where TTokenKind : struct, Enum
    {
        // In many cases TValue could be List<TState>, but it is better to be safe than sorry
        private readonly Dictionary<SourceTransitionPair<ProductionItem<TTokenKind>, Symbol>, HashSet<ProductionItem<TTokenKind>>> _delta;

        private readonly HashSet<ProductionItem<TTokenKind>> _acceptStates;
        private readonly HashSet<ProductionItem<TTokenKind>> _states;
        private readonly Set<Symbol> _alphabet;

        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        public LrItemNfa(
            IEnumerable<Transition<Symbol, ProductionItem<TTokenKind>>> transitions,
            ProductionItem<TTokenKind> startState,
            IEnumerable<ProductionItem<TTokenKind>> acceptStates)
        {
            StartState = startState;
            _acceptStates = new HashSet<ProductionItem<TTokenKind>>(acceptStates);
            _states = new HashSet<ProductionItem<TTokenKind>>(acceptStates) { startState };
            _delta = new Dictionary<SourceTransitionPair<ProductionItem<TTokenKind>, Symbol>, HashSet<ProductionItem<TTokenKind>>>();
            _alphabet = new Set<Symbol>();

            foreach (var triple in transitions)
            {
                _states.Add(triple.SourceState);
                _states.Add(triple.TargetState);
                var pair = Transition.FromPair(triple.SourceState, triple.Label);
                if (_delta.ContainsKey(pair))
                    _delta[pair].Add(triple.TargetState);
                else
                    _delta[pair] = new HashSet<ProductionItem<TTokenKind>> {triple.TargetState};
                if (triple.Label.Equals(Symbol.Epsilon))
                    IsEpsilonNfa = true;
                else
                    _alphabet.Add(triple.Label); // alphabet does not contain epsilon
            }
        }

        public ProductionItem<TTokenKind> StartState { get; }

        public bool IsAcceptState(ProductionItem<TTokenKind> state)
        {
            return _acceptStates.Contains(state);
        }

        public IEnumerable<ProductionItem<TTokenKind>> GetStates()
        {
            return _states;
        }

        public IEnumerable<ProductionItem<TTokenKind>> GetTrimmedStates()
        {
            return GetStates();
        }

        public IEnumerable<Symbol> GetAlphabet()
        {
            return _alphabet;
        }

        public bool IsEpsilonNfa { get; }

        public IEnumerable<ProductionItem<TTokenKind>> GetAcceptStates()
        {
            return _acceptStates;
        }

        public IEnumerable<Transition<Symbol, ProductionItem<TTokenKind>>> GetTransitions()
        {
            foreach (KeyValuePair<SourceTransitionPair<ProductionItem<TTokenKind>, Symbol>, HashSet<ProductionItem<TTokenKind>>> kvp in _delta)
            {
                ProductionItem<TTokenKind> sourceState = kvp.Key.SourceState;
                Symbol label = kvp.Key.Label;
                foreach (ProductionItem<TTokenKind> targetState in kvp.Value)
                {
                    yield return Transition.Move(sourceState, label, targetState);
                }
            }
        }

        public IEnumerable<Transition<Symbol, ProductionItem<TTokenKind>>> GetTrimmedTransitions()
        {
            return GetTransitions();
        }

        /// <summary>
        /// Create Deterministic Finite Automaton by lazy form of subset construction (Rabin and Scott, )
        /// </summary>
        /// <returns>Deterministic Finite Automaton created by lazy form of subset construction</returns>
        public LrItemsDfa<TTokenKind> ToDfa()
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

            var newStartState = EpsilonClose(StartState.AsSingletonEnumerable());

            var newAcceptStates = new HashSet<ProductionItemSet<TTokenKind>>();
            var newTransitions = new List<Transition<Symbol, ProductionItemSet<TTokenKind>>>();

            var newStates = new InsertionOrderedSet<ProductionItemSet<TTokenKind>> {newStartState};

            // Lazy form of Subset Construction where only reachable nodes
            // are added to the following work list of marked subsets
            var markedVisitedStates = new Queue<ProductionItemSet<TTokenKind>>(); // work list that preserves insertion order
            markedVisitedStates.Enqueue(newStartState);

            while (markedVisitedStates.Count != 0)
            {
                // subset S
                ProductionItemSet<TTokenKind> subsetSourceState = markedVisitedStates.Dequeue();

                if (subsetSourceState.Overlaps(GetAcceptStates()))
                {
                    newAcceptStates.Add(subsetSourceState);
                }

                // For all non-epsilon labels
                foreach (var label in GetAlphabet())
                {
                    // subset T
                    var subsetTargetState = new Set<ProductionItem<TTokenKind>>();

                    // kernel items: For all s in S, add all non-epsilon transitions (s, label) → t to T
                    foreach (ProductionItem<TTokenKind> s in subsetSourceState)
                    {
                        subsetTargetState.AddRange(Delta(Transition.FromPair(s, label)));
                    }

                    // Ignore empty subset (implicit transition to dead state in this case)
                    if (subsetTargetState.Count == 0) continue;

                    // Closure Items: Epsilon-close all T such that (S, label) → T
                    var closure = EpsilonClose(subsetTargetState);

                    if (!newStates.Contains(closure))
                    {
                        newStates.Add(closure);
                        markedVisitedStates.Enqueue(closure);
                    }

                    // Add (S, label) → T transition
                    newTransitions.Add(Transition.Move(subsetSourceState, label, closure));
                }
            }

            return new LrItemsDfa<TTokenKind>(newStates, GetAlphabet(), newTransitions, newStartState, newAcceptStates);
        }

        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        ProductionItemSet<TTokenKind> EpsilonClose(IEnumerable<ProductionItem<TTokenKind>> kernelItems)
        {
            var markedVisitedStates = new Queue<ProductionItem<TTokenKind>>(kernelItems);
            // base step
            var closure = new Set<ProductionItem<TTokenKind>>(kernelItems);
            // induction step: recurse until no more states in a round
            while (markedVisitedStates.Count != 0)
            {
                ProductionItem<TTokenKind> sourceState = markedVisitedStates.Dequeue();
                // if any epsilon-moves add them all to the closure (union)
                foreach (var targetState in Delta(Transition.FromPair(sourceState, Symbol.Epsilon)))
                {
                    if (!closure.Contains(targetState))
                    {
                        closure.Add(targetState);
                        markedVisitedStates.Enqueue(targetState);
                    }
                }
            }

            return new ProductionItemSet<TTokenKind>(closure);
        }

        IEnumerable<ProductionItem<TTokenKind>> Delta(SourceTransitionPair<ProductionItem<TTokenKind>, Symbol> pair)
        {
            return _delta.TryGetValue(pair, out var targetStates) ? targetStates : Enumerable.Empty<ProductionItem<TTokenKind>>();
        }
    }
}
