using System.Collections.Generic;
using AutomataLib;

namespace AutomataRepresentations
{
    /// <summary>
    /// We basically have a list of triples (p, a, δ(p, a)) and then the list is stored in a hashing table
    /// defined on pairs (p, a). Then given a pair (p, a) for the state of the system we compute the hash and
    /// access the triple (p, a, δ(p, a)) in constant time.
    ///
    /// Advantages:
    ///   - The alphabet does not have to indexable
    ///   - The alphabet does not have to be defined as fixed in advance
    ///   - Only transitions of trimmed DFA need to be in RAM
    /// Disadvantage
    ///    - Slower transition function, means slower recognize algorithm
    /// </summary>
    public class DfaHashedAdjacencyList<TState>
    {
        // Store triple (s1, a, s2) transitions in a list and use hashing on (s1, a) to retrieve the full transitions
        private readonly Dictionary<SourceTransitionPair<int, char>, int> _nextState = null; // TODO

        public DfaHashedAdjacencyList(
            IEnumerable<TState> states, // should be unique...we do not test this here
            IEnumerable<char> alphabet, // should be unique...we do not test this here
            IEnumerable<Transition<char, TState>> transitions,
            TState startState,
            IEnumerable<TState> acceptingStates)
        {

        }

        public int NextState(int s, char c)
        {
            return _nextState.TryGetValue(Transition.FromPair(s, c), out int nextState)
                ? nextState
                : 0; // default transition is to the dead state
        }
    }
}
