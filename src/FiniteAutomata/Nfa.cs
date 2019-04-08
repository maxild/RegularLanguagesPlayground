using System;
using System.Collections.Generic;
using System.Linq;
using AutomataLib;

namespace FiniteAutomata
{
    // Idea for testing finite/infinite DFA
    // 1. Eliminate states that are not reachable
    // 2. Eliminate states that do not reach a final state
    // 3. Test if the remaining states have eny cycles

    // Emptiness
    // No final state is reachable (depth/breadth first algorithms)

    // TODO: Create (membership) Matches method on NFA (simulate it, without converting to DFA)

    /// <summary>
    /// Class Nfa and conversion from NFA to DFA
    /// </summary>
    /// <remarks>
    /// A non-deterministic finite automaton (NFA) is represented as a
    /// Map from state number (int) to a List of Transitions, a
    /// Transition being a pair of a label label (a string, null meaning
    /// epsilon) and a target state (an int).
    /// </remarks>
    /// <remarks>
    /// A DFA is created from an NFA in two steps:
    ///
    ///  (1) Construct a DFA whose each of whose states is composite,
    ///      namely a set of NFA states (Set of int).  This is done by
    ///      methods CompositeDfaTrans and EpsilonClose.
    ///
    ///  (2) Replace composite states (Set of int) by simple states
    ///      (int). This is done by methods Rename and MkRenamer.
    /// </remarks>
    public class Nfa<TAlphabet>
        where TAlphabet : IEquatable<TAlphabet>
    {
        private readonly Func<int, bool> _predicate;

        /// <summary>
        /// Create NFA machine building block.
        /// </summary>
        /// <param name="startState">Start state (Thompson)</param>
        /// <param name="acceptState">Exit state (Thompson)</param>
        public Nfa(int startState, int acceptState)
            : this(startState, new []{acceptState}, null)
        {
        }

        public Nfa(int startState, IEnumerable<int> acceptingStates, Func<int, bool> predicate)
        {
            Start = startState;
            AcceptingStates = new Set<int>(acceptingStates);
            Trans = new Dictionary<int, List<TargetTransitionPair<TAlphabet, int>>>();
            // AddTrans(s1, a, s2) will add new list to any start state s1, but not s2 exit states
            foreach (var acceptingState in AcceptingStates)
            {
                if (!startState.Equals(acceptingState))
                    Trans.Add(acceptingState, new List<TargetTransitionPair<TAlphabet, int>>());
            }

            _predicate = predicate;
        }

        public int Start { get; }

        public Set<int> AcceptingStates { get; } // TODO: ImmutableSet

        public int GetRequiredSingleAcceptingState()
        {
            if (AcceptingStates.Count != 1)
            {
                throw new InvalidOperationException("The NFA does not have a single accepting state.");
            }
            int[] buffer = new int[1];
            AcceptingStates.CopyTo(buffer, 0);
            return buffer[0];
        }

        /// <summary>
        /// For any state we can find a list of transitions -- i.e.
        /// Trans[q] is a list of pairs {(a, p1), (a, p2), (b, p1),....}
        /// where the pair (a, p1) show us that on input 'a' we move
        /// to state p1. We use a list because the same input can be in
        /// many pairs (non-deterministic) machine.
        /// </summary>
        // TODO: It should be private implementation detail how the DFA is implemented
        public IDictionary<int, List<TargetTransitionPair<TAlphabet, int>>> Trans { get; }

        public void AddTrans(Transition<TAlphabet, int> t)
        {
            List<TargetTransitionPair<TAlphabet, int>> transitions;
            if (Trans.ContainsKey(t.SourceState))
            {
                transitions = Trans[t.SourceState];
            }
            else
            {
                transitions = new List<TargetTransitionPair<TAlphabet, int>>();
                Trans.Add(t.SourceState, transitions);
            }

            // TODO: It should not be possible to add the same pair (label, toState) twice.
            // Maybe HashSet of moves/transitions, BUT List is so much easier to grasp and
            // {(a,1),(a,3),(b,5),(a,3)} is equal equal to {(b,5),(a,1),(a,3)}, because order
            // of elements and duplicate elements does not matter.
            transitions.Add(Transition.ToPair(t.Label, t.TargetState));
        }

        public void AddTrans(KeyValuePair<int, List<TargetTransitionPair<TAlphabet, int>>> tr)
        {
            // Assumption: if tr is in trans, it maps to an empty list (end state)
            Trans.Remove(tr.Key);
            Trans.Add(tr.Key, tr.Value);
        }

        public override string ToString()
        {
            return "NFA start state is " + Start + ", Accepting states are " + AcceptingStates;
        }

        // Construct the transition relation of a composite-state DFA
        // from an NFA with start state s0 and transition relation
        // trans (a Map from int to List of Transition).  The start
        // state of the constructed DFA is the epsilon closure of s0,
        // and its transition relation is a Map from a composite state
        // (a Set of ints) to a Map from label (a string) to a
        // composite state (a Set of ints).
        //
        //Method CompositeDfaTrans works as follows:
        //
        //      1. Create the epsilon-closure S0 (a Set of ints) of the start
        //      state s0, and put it in a worklist (a Queue).  Create an
        //      empty DFA transition relation, which is a Map from a
        //      composite state (an epsilon-closed Set of ints) to a Map
        //      from a label (a non-null string) to a composite state.
        //
        //      2. Repeatedly choose a composite state S from the worklist.  If it is
        //      not already in the keyset of the DFA transition relation, compute
        //      for every non-epsilon label label the set T of states reachable by
        //      that label from some state s in S.  Compute the epsilon-closure
        //      Tclose of every such state T and put it on the worklist.  Then add
        //      the transition S -label-> Tclose to the DFA transition relation, for
        //      every label.

        static IDictionary<Set<int>, IDictionary<TAlphabet, Set<int>>> CompositeDfaTrans(
            int startState,
            IDictionary<int, List<TargetTransitionPair<TAlphabet, int>>> trans)
        {
            // Lazy form of Subset Construction where only reachable nodes are converted

            // CL(s0), where s0 is singleton start state
            Set<int> s0EpsClosure = EpsilonClose(new Set<int>(new [] {startState}), trans);
            var markedVisitedStates = new Queue<Set<int>>();
            markedVisitedStates.Enqueue(s0EpsClosure);

            // The transition relation of the DFA = (States, Transition)
            var result = new Dictionary<Set<int>, IDictionary<TAlphabet, Set<int>>>();

            while (markedVisitedStates.Count != 0)
            {
                Set<int> subset = markedVisitedStates.Dequeue();
                if (!result.ContainsKey(subset))
                {
                    // The (S, label) -> T transition relation being constructed for a given S
                    IDictionary<TAlphabet, Set<int>> subsetTrans =
                        new Dictionary<TAlphabet, Set<int>>();

                    // For all s in S, consider all transitions (s, label) -> t
                    foreach (int s in subset)
                    {
                        // For all non-epsilon transitions s -label-> t, add t to T
                        foreach (var tr in trans[s])
                        {
                            if (tr.Label != null) // not epsilon
                            {
                                Set<int> toState;
                                if (subsetTrans.ContainsKey(tr.Label))
                                {
                                    // Already a transition on label
                                    toState = subsetTrans[tr.Label];
                                }
                                else
                                {
                                    // No transitions on label yet
                                    toState = new Set<int>();
                                    subsetTrans.Add(tr.Label, toState);
                                }

                                toState.Add(tr.TargetState);
                            }
                        }
                    }

                    // Epsilon-close all T such that (S, label) -> T, and put on worklist
                    Dictionary<TAlphabet, Set<int>> subsetTransClosed =
                        new Dictionary<TAlphabet, Set<int>>();
                    foreach (KeyValuePair<TAlphabet, Set<int>> entry in subsetTrans)
                    {
                        Set<int> toSubsetEpsClosure = EpsilonClose(entry.Value, trans);
                        subsetTransClosed.Add(entry.Key, toSubsetEpsClosure);
                        markedVisitedStates.Enqueue(toSubsetEpsClosure);
                    }

                    result.Add(subset, subsetTransClosed);
                }
            }

            return result;
        }

        /// <summary>
        /// Compute epsilon-closure of a set of states
        /// </summary>
        /// <param name="states">The set of states to closure</param>
        /// <param name="trans">The transitions of the NFA.</param>
        /// <returns>The epsilon closure of the given states.</returns>
        static Set<int> EpsilonClose(Set<int> states, IDictionary<int, List<TargetTransitionPair<TAlphabet, int>>> trans)
        {
            var markedVisitedStates = new Queue<int>(states); // mark visited states
            var result = new Set<int>(states);
            while (markedVisitedStates.Count != 0)
            {
                int s = markedVisitedStates.Dequeue();
                foreach (var tr in trans[s])
                {
                    // TODO: Create better representation of single letters and empty string (epsilon)
                    if (tr.Label == null && !result.Contains(tr.TargetState))
                    {
                        result.Add(tr.TargetState);
                        markedVisitedStates.Enqueue(tr.TargetState);
                    }
                }
            }
            return result;
        }

        // Using a renamer (a Map from Set of int to int), replace
        // composite (Set of int) states with simple (int) states in
        // the transition relation trans, which is assumed to be a Map
        // from Set of int to Map from string to Set of int.  The
        // result is a Map from int to Map from string to int.

        // Given a Map from Set of int to Map from string to Set of
        // int, use the result of MkRenamer to replace all Sets of ints
        // by ints.

        static IDictionary<int, IDictionary<TAlphabet, int>> Rename(
            NfaToDfaRenamer renamer,
            IDictionary<Set<int>, IDictionary<TAlphabet, Set<int>>> dfaTrans)
        {
            var newDfaTrans = new SortedDictionary<int, IDictionary<TAlphabet, int>>(); // keys/states are sorted

            foreach (KeyValuePair<Set<int>, IDictionary<TAlphabet, Set<int>>> entry
                in dfaTrans)
            {
                Set<int> dfaState = entry.Key;
                var newDfaTransRow = new Dictionary<TAlphabet, int>();
                foreach (KeyValuePair<TAlphabet, Set<int>> tr in entry.Value)
                {
                    newDfaTransRow.Add(tr.Key, renamer.ToDfaStateIndex(tr.Value));
                }
                newDfaTrans.Add(renamer.ToDfaStateIndex(dfaState), newDfaTransRow);
            }

            return newDfaTrans;
        }

        static Set<int> AcceptStates(
            ICollection<Set<int>> states,
            NfaToDfaRenamer renamer,
            Set<int> acceptingStates)
        {
            Set<int> acceptStates = new Set<int>();
            foreach (Set<int> dfaSubsetState in states)
            {
                if (acceptingStates.Any(finalState => dfaSubsetState.Contains(finalState)))
                    acceptStates.Add(renamer.ToDfaStateIndex(dfaSubsetState));

            }
            return acceptStates;
        }

        /// <summary>
        /// McNaughton-Yamada-Thompson algorithm (aka Thompson's construction)
        /// </summary>
        public Dfa<TAlphabet> ToDfa(bool skipRenaming = false)
        {
            IDictionary<Set<int>, IDictionary<TAlphabet, Set<int>>>
                cDfaTrans = CompositeDfaTrans(Start, Trans);

            Set<int> cDfaStart = EpsilonClose(new Set<int>(new [] {Start}), Trans);

            ICollection<Set<int>> cDfaStates = cDfaTrans.Keys;

            var renamer = new NfaToDfaRenamer(cDfaStates, skipRenaming, _predicate);

            // DFA-transitions (delta)
            IDictionary<int, IDictionary<TAlphabet, int>> dfaTrans =
                Rename(renamer, cDfaTrans);

            // The singleton start state = q_0
            int dfaStartStateIndex = renamer.ToDfaStateIndex(cDfaStart);

            // The subset of accepting states = F
            Set<int> dfaAcceptingStateIndices = AcceptStates(cDfaStates, renamer, AcceptingStates);

            return new Dfa<TAlphabet>(dfaStartStateIndex, dfaAcceptingStateIndices, dfaTrans, renamer);
        }
    }

    /// <summary>
    /// Convert to and from int.
    /// </summary>
    public interface IDfaStateRenamer
    {
        string ToDfaStateString(int dfaStateIndex);
    }

    /// <summary>
    /// Given a Map from Set of int to something, create an
    /// injective Map from Set of int to int, by choosing a fresh
    /// int for every value of the map.
    /// </summary>
    public class NfaToDfaRenamer : IDfaStateRenamer
    {
        private readonly Dictionary<Set<int>, int> _nfaStatesToDfaState;
        private readonly List<Set<int>> _dfaStateToNfaStates;
        private readonly Func<int, bool> _predicate;
        private readonly bool _skipRenaming;

        public NfaToDfaRenamer(ICollection<Set<int>> dfaStates, bool skipRenaming, Func<int, bool> predicate = null)
        {
            _nfaStatesToDfaState = new Dictionary<Set<int>, int>(dfaStates.Count);
            _dfaStateToNfaStates = new List<Set<int>>(dfaStates.Count);
            int count = 0;
            foreach (Set<int> k in dfaStates)
            {
                int nfaStateIndex = count;
                _nfaStatesToDfaState.Add(k, nfaStateIndex);
                _dfaStateToNfaStates.Add(k);
                count += 1;
            }

            _skipRenaming = skipRenaming;

            _predicate = predicate ?? (_ => true);
        }

        public int ToDfaStateIndex(Set<int> nfaStates)
        {
            return _nfaStatesToDfaState[nfaStates];
        }

        public string ToDfaStateString(int dfaStateIndex)
        {
            return _skipRenaming
                ? dfaStateIndex.ToString()
                : _dfaStateToNfaStates[dfaStateIndex].Where(_predicate).ToSetNotation();
        }
    }
}
