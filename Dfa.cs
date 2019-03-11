using System.Collections.Generic;
using System.IO;

namespace RegExpToDfa
{
    // TODO: Make Match(string s) : bool

    /// <summary>
    /// A deterministic finite automaton (DFA) is represented as a Map
    /// from state number (int) to a Map from label (a String,
    /// non-null) to a target state (an int).
    /// </summary>
    public class Dfa
    {
        private readonly NfaToDfaRenamer _renamer;

        public Dfa(
            int startState,
            Set<int> acceptStates,
            IDictionary<int, IDictionary<string, int>> trans,
            NfaToDfaRenamer renamer)
        {
            Start = startState;
            Accept = acceptStates;
            Trans = trans;
            _renamer = renamer;
        }

        public int Start { get; }

        public Set<int> Accept { get; }

        public IDictionary<int, IDictionary<string, int>> Trans { get; }

        public override string ToString()
        {
            return "DFA start=" + Start + "\naccept=" + Accept;
        }

        public bool Match(string s)
        {
            int state = Start;
            foreach (char c in s)
            {
                string input = new string(c, 1);
                if (Trans[state].TryGetValue(input, out int newState))
                {
                    state = newState;
                }
                else
                {
                    // dead state
                    return false;
                }
            }

            return Accept.Contains(state);
        }

        // Write an input file for the dot program.  You can find dot at
        // http://www.research.att.com/sw/tools/graphviz/

        public void WriteDot(string path)
        {
            using (var fileStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write))

            using (var sw = new StreamWriter(fileStream))
            {
                sw.WriteLine("// Format this file as a Postscript file with ");
                sw.WriteLine("//    dot " + path + " -Tps -o out.ps\n");
                sw.WriteLine("digraph dfa {");
                sw.WriteLine("size=\"11,8.25\";");
                //sw.WriteLine("rotate=90;");
                sw.WriteLine("rankdir=LR;");
                sw.WriteLine("n999999 [style=invis];"); // Invisible start node
                sw.WriteLine("n999999 -> n" + Start); // Edge into start state

                // labels that indicate the NFA states of the subset construction
                foreach (int state in Trans.Keys)
                    sw.WriteLine("n" + state + " [label=\"" + _renamer.ToDfaStateString(state) + "\"]");

                // Accept states are double circles
                foreach (int state in Trans.Keys)
                    if (Accept.Contains(state))
                        sw.WriteLine("n" + state + " [peripheries=2];");

                // The transitions
                foreach (KeyValuePair<int, IDictionary<string, int>> entry in Trans)
                {
                    int fromState = entry.Key; // from-state
                    foreach (KeyValuePair<string, int> s1Trans in entry.Value)
                    {
                        string input = s1Trans.Key;
                        int toState = s1Trans.Value;
                        sw.WriteLine("n" + fromState + " -> n" + toState + " [label=\"" + input + "\"];");
                    }
                }

                sw.WriteLine("}");

                // Ensure we overwrite an existing file
                fileStream.SetLength(fileStream.Position);
            }

        }
    }

}
