using System.Collections.Generic;

namespace AutomataLib
{
    public interface INondeterministicFiniteAutomaton<TAlphabet, TState> : IFiniteAutomaton<TAlphabet, TState>
    {
        /// <summary>
        /// Alphabet with any epsilon
        /// </summary>
        IEnumerable<TAlphabet> GetNullableAlphabet();

        bool IsEpsilonNfa { get; }
    }
}
