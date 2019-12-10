using System.Collections.Generic;

namespace AutomataLib
{
    public interface IDeterministicFiniteAutomaton<TAlphabet, TState> : IFiniteAutomaton<TAlphabet, TState>
    {
        TState TransitionFunction(TState state, IEnumerable<TAlphabet> input);

        bool IsMatch(IEnumerable<TAlphabet> input);
    }
}
