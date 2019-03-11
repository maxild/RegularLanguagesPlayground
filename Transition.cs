namespace RegExpToDfa
{
    /// <summary>
    /// A transition to another state on an input (i.e. args to transition function).
    /// </summary>
    public class Transition
    {
        public string Lab;
        public int Target;

        /// <summary>
        /// Initialize transition
        /// </summary>
        /// <param name="lab">Input (label???) that transitions to target (null meaning epsilon)</param>
        /// <param name="target">Target state</param>
        public Transition(string lab, int target)
        {
            Lab = lab;
            Target = target;
        }

        public override string ToString()
        {
            return "-" + Lab + "-> " + Target;
        }
    }
}