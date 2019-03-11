using System;
using System.Collections.Generic;
using System.Linq;

namespace RegExpToDfa
{
    /// <summary>
    /// Class Nfa and conversion from NFA to DFA
    /// </summary>
    /// <remarks>
    /// A non-deterministic finite automaton (NFA) is represented as a
    /// Map from state number (int) to a List of Transitions, a
    /// Transition being a pair of a label lab (a string, null meaning
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
    public class Nfa
    {
        private readonly Func<int, bool> _predicate;

        public Nfa(int startState, int exitState)
        {
            Start = startState;
            AcceptingStates = new Set<int>(exitState);
            // for every state
            Trans = new Dictionary<int, List<Transition>>();
            // AddTrans(s1, a, s2) will add new list to any start state s1, but not s2 exit states
            if (!startState.Equals(exitState))
                Trans.Add(exitState, new List<Transition>());
        }

        public Nfa(int startState, IEnumerable<int> acceptingStates, Func<int, bool> predicate)
        {
            Start = startState;
            AcceptingStates = new Set<int>(acceptingStates);
            Trans = new Dictionary<int, List<Transition>>();
            // AddTrans(s1, a, s2) will add new list to any start state s1, but not s2 exit states
            foreach (var acceptingState in AcceptingStates)
            {
                if (!startState.Equals(acceptingState))
                    Trans.Add(acceptingState, new List<Transition>());
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
        /// Trans[q] is a list of pairs {(a, p1), (a,p2), (b, p1),....}
        /// where the pair (a, p1) show us that on on input 'a' we move
        /// to state p1. We use a list because the same input can be any
        /// many pairs (non-deterministic) machine.
        /// </summary>
        public IDictionary<int, List<Transition>> Trans { get; }

        public void AddTrans(int s1, string lab, int s2)
        {
            List<Transition> transitions;
            if (Trans.ContainsKey(s1))
            {
                transitions = Trans[s1];
            }
            else
            {
                transitions = new List<Transition>();
                Trans.Add(s1, transitions);
            }

            transitions.Add(new Transition(lab, s2));
        }

        public void AddTrans(KeyValuePair<int, List<Transition>> tr)
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
        //      for every non-epsilon label lab the set T of states reachable by
        //      that label from some state s in S.  Compute the epsilon-closure
        //      Tclose of every such state T and put it on the worklist.  Then add
        //      the transition S -lab-> Tclose to the DFA transition relation, for
        //      every lab.

        static IDictionary<Set<int>, IDictionary<string, Set<int>>> CompositeDfaTrans(
            int s0,
            IDictionary<int, List<Transition>> trans)
        {
            // CL(s0), where s0 is singleton start state
            Set<int> S0 = EpsilonClose(new Set<int>(s0), trans); // startState

            Queue<Set<int>> worklist = new Queue<Set<int>>();
            worklist.Enqueue(S0);

            // The transition relation of the DFA = (States, Transition)
            IDictionary<Set<int>, IDictionary<string, Set<int>>> res =
                new Dictionary<Set<int>, IDictionary<string, Set<int>>>();

            while (worklist.Count != 0)
            {
                Set<int> S = worklist.Dequeue();
                if (!res.ContainsKey(S))
                {
                    // The (S, lab) -> T transition relation being constructed for a given S
                    IDictionary<string, Set<int>> STrans =
                        new Dictionary<string, Set<int>>();

                    // For all s in S, consider all transitions (s, lab) -> t
                    foreach (int s in S)
                    {
                        // For all non-epsilon transitions s -lab-> t, add t to T
                        foreach (Transition tr in trans[s])
                        {
                            if (tr.Input != null) // not epsilon
                            {
                                Set<int> toState;
                                if (STrans.ContainsKey(tr.Input))
                                {
                                    // Already a transition on lab
                                    toState = STrans[tr.Input];
                                }
                                else
                                {
                                    // No transitions on lab yet
                                    toState = new Set<int>();
                                    STrans.Add(tr.Input, toState);
                                }

                                toState.Add(tr.ToState);
                            }
                        }
                    }

                    // Epsilon-close all T such that (S, lab) -> T, and put on worklist
                    Dictionary<string, Set<int>> STransClosed =
                        new Dictionary<string, Set<int>>();
                    foreach (KeyValuePair<string, Set<int>> entry in STrans)
                    {
                        Set<int> Tclose = EpsilonClose(entry.Value, trans);
                        STransClosed.Add(entry.Key, Tclose);
                        worklist.Enqueue(Tclose);
                    }

                    res.Add(S, STransClosed);
                }
            }

            return res;
        }

        /// <summary>
        /// Compute epsilon-closure of a set of states
        /// </summary>
        /// <param name="states">The set of states to closure</param>
        /// <param name="trans">The transitions of the NFA.</param>
        /// <returns>The epsilon closure of the given states.</returns>
        static Set<int> EpsilonClose(Set<int> states, IDictionary<int, List<Transition>> trans)
        {
            Queue<int> worklist = new Queue<int>(states);
            Set<int> result = new Set<int>(states);
            while (worklist.Count != 0)
            {
                int s = worklist.Dequeue();
                foreach (Transition tr in trans[s])
                {
                    if (tr.Input == null && !result.Contains(tr.ToState))
                    {
                        result.Add(tr.ToState);
                        worklist.Enqueue(tr.ToState);
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

        static IDictionary<int, IDictionary<string, int>> Rename(
            NfaToDfaRenamer renamer,
            IDictionary<Set<int>, IDictionary<string, Set<int>>> dfaTrans)
        {
            var newDfaTrans = new Dictionary<int, IDictionary<string, int>>();

            foreach (KeyValuePair<Set<int>, IDictionary<string, Set<int>>> entry
                in dfaTrans)
            {
                Set<int> dfaState = entry.Key;
                IDictionary<string, int> newDfaTransRow = new Dictionary<string, int>();
                foreach (KeyValuePair<string, Set<int>> tr in entry.Value)
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

        public Dfa ToDfa()
        {
            IDictionary<Set<int>, IDictionary<string, Set<int>>>
                cDfaTrans = CompositeDfaTrans(Start, Trans);

            Set<int> cDfaStart = EpsilonClose(new Set<int>(Start), Trans);

            ICollection<Set<int>> cDfaStates = cDfaTrans.Keys;

            var renamer = new NfaToDfaRenamer(cDfaStates, _predicate);

            // DFA-transitions (delta)
            IDictionary<int, IDictionary<string, int>> dfaTrans =
                Rename(renamer, cDfaTrans);

            // The singleton start state = q_0
            int dfaStartStateIndex = renamer.ToDfaStateIndex(cDfaStart);

            // The subset of accepting states = F
            Set<int> dfaAcceptingStateIndices = AcceptStates(cDfaStates, renamer, AcceptingStates);

            return new Dfa(dfaStartStateIndex, dfaAcceptingStateIndices, dfaTrans, renamer);
        }

        /// <summary>
        /// Nested class for creating distinctly named states when constructing NFAs
        /// </summary>
        public class NameSource
        {
            private static int _nextName;

            public int Next()
            {
                return _nextName++;
            }
        }
    }

    /// <summary>
    /// Given a Map from Set of int to something, create an
    /// injective Map from Set of int to int, by choosing a fresh
    /// int for every value of the map.
    /// </summary>
    public class NfaToDfaRenamer
    {
        private readonly Dictionary<Set<int>, int> _nfaStatesToDfaState;
        private readonly List<Set<int>> _dfaStateToNfaStates;
        private readonly Func<int, bool> _predicate;

        public NfaToDfaRenamer(ICollection<Set<int>> dfaStates, Func<int, bool> predicate = null)
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

            _predicate = predicate ?? (_ => true);
        }

        public int ToDfaStateIndex(Set<int> nfaStates)
        {
            return _nfaStatesToDfaState[nfaStates];
        }

        public string ToDfaStateString(int dfaStateIndex)
        {

            return _dfaStateToNfaStates[dfaStateIndex].Where(_predicate).ToSetNotation();
        }
    }
}
