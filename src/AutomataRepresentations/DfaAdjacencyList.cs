using System.Collections.Generic;
using AutomataLib;

namespace AutomataRepresentations
{
    /// <summary>
    /// Adjacency-list of pairs (a, δ(p, a)) defined for every state a in Q. The alphabet does not have to be indexable,
    /// it just need to be equatable. The space complexity may be further reduced by considering a default (target)
    /// state associated to each adjacency list (the most frequently occurring target of a given adjacency list is
    /// an obvious choice as default for this adjacency list). We have chosen the error state as the default state.
    ///
    /// Advantages:
    ///   - The alphabet does not have to indexable
    ///   - The alphabet does not have to be defined as fixed in advance
    ///   - Only transitions of trimmed DFA need to be in RAM
    /// Disadvantage
    ///    - Slower transition function, means slower recognize algorithm
    /// </summary>
    public class DfaAdjacencyList<TState>
    {
        // each state has an adjacency list of character-state pairs,
        // and if label is not on the list we go to state 0 (error state)
        private TargetTransitionPair<char, int>[][] _nextState = null; // jagged array of (label, next)-pairs

        public DfaAdjacencyList(
            IEnumerable<TState> states, // should be unique...we do not test this here
            IEnumerable<char> alphabet, // should be unique...we do not test this here
            IEnumerable<Transition<char, TState>> transitions,
            TState startState,
            IEnumerable<TState> acceptingStates)
        {

        }

        private int NextState(int s, char c)
        {
            // linear search for input symbol: could be a binary search if we
            // sorted the list (important if we have many transitions on states)
            for (int i = 0; i < _nextState[s].Length; i++)
            {
                if (_nextState[s][i].Label == c)
                {
                    return _nextState[s][i].TargetState;
                }
            }

            return 0; // default transition is to the dead state
        }
    }
}
