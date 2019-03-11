namespace RegExpToDfa
{
    /// <summary>
    /// A transition to another state on a given input.
    /// </summary>
    public struct Transition
    {
        /// <summary>
        /// Input character
        /// </summary>
        public string Input;

        /// <summary>
        /// State we transition into on <see cref="Input"/>
        /// </summary>
        public int ToState;

        public Transition(string input, int toState)
        {
            Input = input;
            ToState = toState;
        }

        public override string ToString()
        {
            return "-" + Input + "-> " + ToState;
        }
    }
}
