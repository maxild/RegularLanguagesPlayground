using System.Collections.Generic;

namespace AutomataRepresentations
{
    public interface IDeterministicFiniteAutomaton : IFiniteAutomaton
    {
        int TransitionFunction(int state, IEnumerable<char> input);

        bool IsMatch(string input);
    }
}
