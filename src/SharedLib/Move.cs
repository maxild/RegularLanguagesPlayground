namespace AutomataLib
{
    /// <summary>
    /// Directed edge in digraph that represents a labeled transition in the digraph.
    /// </summary>
    /// <typeparam name="TAlphabet">the alphabet type of the labels</typeparam>
    public class Move<TAlphabet>
    {
        public readonly int SourceState;
        public readonly int TargetState;
        public readonly TAlphabet Label;

        /// <summary>
        /// Transition of an automaton.
        /// </summary>
        public Move(int sourceState, int targetState, TAlphabet label)
        {
            SourceState = sourceState;
            TargetState = targetState;
            Label = label;
        }

        /// <summary>
        /// Creates a move. Creates an epsilon move if label is default(L).
        /// </summary>
        public static Move<TAlphabet> Create(int sourceState, int targetState, TAlphabet label)
        {
            return new Move<TAlphabet>(sourceState, targetState, label);
        }

        /// <summary>
        /// Creates an epsilon move. Same as Create(sourceState, targetState, default(L)).
        /// </summary>
        public static Move<TAlphabet> Epsilon(int sourceState, int targetState)
        {
            return new Move<TAlphabet>(sourceState, targetState, default(TAlphabet));
        }

        // TODO: Er dette med default(TLabel) smart (char 0)???
        public bool IsEpsilon => object.Equals(Label, default(TAlphabet));

        public override bool Equals(object obj)
        {
            if (!(obj is Move<TAlphabet>))
                return false;
            var t = (Move<TAlphabet>)obj;
            return t.SourceState == SourceState &&
                   t.TargetState == TargetState &&
                   t.IsEpsilon ? IsEpsilon : t.Label.Equals(Label);
        }

        public override int GetHashCode()
        {
            return SourceState + TargetState * 2 + (IsEpsilon ? 0 : Label.GetHashCode());
        }

        public override string ToString()
        {
            return "(" + SourceState + "," + (IsEpsilon ? "" : Label + ",") + TargetState + ")";
        }
    }
}
