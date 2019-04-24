namespace AutomataLib
{
    public interface INondeterministicFiniteAutomaton<TAlphabet, TState> : IFiniteAutomaton<TAlphabet, TState>
    {
        bool IsEpsilonNfa { get; }
    }
}
