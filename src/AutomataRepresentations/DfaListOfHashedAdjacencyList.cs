using System.Collections.Generic;
using AutomataLib;

namespace AutomataRepresentations
{
    // DfaHashedAdjacencyList is probably better
    public class DfaListOfHashedAdjacencyList<TState>
    {
        // Fixed set of indexable states
        // Store list of pairs {(a, s2) for all a in Alphabet} indexed by source state.
        // Then use hashing on input symbol 'a' to retrieve the full pair ('a', s2)
        private readonly Dictionary<char, int>[] _nextState = null;
        // TODO: Is it expensive to have a dictionary (hash map) for every state?

        public DfaListOfHashedAdjacencyList(
            IEnumerable<TState> states, // should be unique...we do not test this here
            IEnumerable<char> alphabet, // should be unique...we do not test this here
            IEnumerable<Transition<char, TState>> transitions,
            TState startState,
            IEnumerable<TState> acceptStates)
        {

        }

        public int NextState(int s, char c)
        {
            return _nextState[s].TryGetValue(c, out int nextState)
                ? nextState
                : 0; // default transition is to the dead state
        }
    }
}
