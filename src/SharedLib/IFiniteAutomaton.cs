using System.Collections.Generic;

namespace AutomataLib
{
    // Online Learning Tools
    // https://aude.imag.fr/aude/
    // http://automatatutor.com/about/  (http://pages.cs.wisc.edu/~loris/teaching.html)

    public interface IFiniteAutomaton<TAlphabet, TState>
    {
        TState StartState { get; }

        bool IsAcceptState(TState state);

        IEnumerable<TState> GetStates();
        IEnumerable<TState> GetTrimmedStates();

        //IEnumerable<TAlphabet> GetAlphabet();

        IEnumerable<TState> GetAcceptStates();

        IEnumerable<Transition<TAlphabet, TState>> GetTransitions();
        IEnumerable<Transition<TAlphabet, TState>> GetTrimmedTransitions();
    }

    public static class FiniteAutomatonExtensions
    {
        public static string GetStateId<TAlphabet, TState>(this IFiniteAutomaton<TAlphabet, TState> fa, TState state)
        {
            return state is IFiniteAutomatonState faState
                ? faState.Id
                : state.ToString();
        }

        public static string GetStateLabel<TAlphabet, TState>(this IFiniteAutomaton<TAlphabet, TState> fa, TState state)
        {
            return state is IFiniteAutomatonState faState
                ? faState.Label // state type have an opinion
                : fa is IFiniteAutomatonStateHomomorphism<TState> faStateH
                    ? faStateH.GetStateLabel(state) // automaton have en opinion
                    : state.ToString();
        }
    }
}
