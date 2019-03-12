namespace RegExpToDfa
{
    /// <summary>
    /// A transition to another state on a given label (input) in the transition graph (transition table)
    /// </summary>
    public struct Transition
    {
        /// <summary>
        /// Input (character) that labels the transition
        /// </summary>
        public string Label;

        /// <summary>
        /// State we transition into on <see cref="Label"/>
        /// </summary>
        public int ToState;

        public Transition(string label, int toState)
        {
            Label = label;
            ToState = toState;
        }

        public override string ToString()
        {
            return "-" + Label + "-> " + ToState;
        }
    }
}
