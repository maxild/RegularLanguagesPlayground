namespace AutomataLib
{
    public interface IFiniteAutomatonStateHomomorphism<in TState>
    {
        /// <summary>
        /// The automaton have state represented by <code>TState</code>, but the user
        /// should see another representation in the transition graph.
        /// </summary>
        string GetStateLabel(TState state);
    }
}
