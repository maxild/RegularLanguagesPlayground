using System.Collections.Generic;
using AutomataLib;

namespace AutomataRepresentations
{
    public class DfaHashedAdjacencyList<TState>
    {
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
            return _nextState.TryGetValue(Transition.FromPair(s, c), out var nextState) ? nextState : 0;
        }
    }
}