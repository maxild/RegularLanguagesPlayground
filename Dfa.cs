using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace RegExpToDfa
{
    // TODO: Make Match(string s) : bool

    // Note: Keys of dictionary transitions are sorted such that states are ordered 0,1,2,3,4 (or whatever)

    /// <summary>
    /// A deterministic finite automaton (DFA) is represented as a Map
    /// from state number (int) to a Map from label (a String,
    /// non-null) to a target state (an int).
    /// </summary>
    public class Dfa
    {
        private readonly IDfaStateRenamer _renamer;

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

        /// <summary>
        /// Naming states with ASCII letters
        /// </summary>
        public Dfa(
            char startState,
            IEnumerable<char> acceptingStates)
        {
            Start = startState;
            Accept = new Set<int>(acceptingStates.Select(x => (int)x));
            Trans = new SortedDictionary<int, IDictionary<string, int>>(); // keys, states are sorted
            _renamer = new CharStateRenamer();
        }

        public int Start { get; }

        public Set<int> Accept { get; }

        public IDictionary<int, IDictionary<string, int>> Trans { get; }

        public void AddTrans(int s1, string label, int s2)
        {
            IDictionary<string, int> transitions;
            if (Trans.ContainsKey(s1))
            {
                transitions = Trans[s1];
            }
            else
            {
                transitions = new Dictionary<string, int>();
                Trans.Add(s1, transitions);
            }
            transitions.Add(label, s2);
        }

        // TODO: Sikkert ikke noedvendig...slet den
        //public void AddTrans(char s1, string label, char s2)
        //{
        //    IDictionary<string, int> transitions;
        //    if (Trans.ContainsKey(s1))
        //    {
        //        transitions = Trans[s1];
        //    }
        //    else
        //    {
        //        transitions = new Dictionary<string, int>();
        //        Trans.Add(s1, transitions);
        //    }
        //    transitions.Add(label, s2);
        //}

        public override string ToString()
        {
            return $"DFA start = {_renamer.ToDfaStateString(Start)}, Accept = " +
                   Accept.Select(x => _renamer.ToDfaStateString(x)).ToSetNotation();
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

        // For debugging
        public string DisplayEquivalentPairs()
        {
            return GetEquivalentPairs()
                .Select(p => new TriangularPair<string>(_renamer.ToDfaStateString(p.Fst), _renamer.ToDfaStateString(p.Snd)))
                .ToSetNotation();
        }

        TriangularPair<int>[] GetEquivalentPairs()
        {
            // table filling algorithm:

            var undistinguishablePairs = new Set<TriangularPair<int>>(); // not marked!!!

            // basis
            foreach (int p in Trans.Keys)
            {
                foreach (int q in Trans.Keys)
                {
                    if (q <= p) continue; // triangular: we use the convention firstIndex < secondIndex
                    if (!IsBasisDistinguishable(p, q))
                    {
                        undistinguishablePairs.Add(new TriangularPair<int>(p, q));
                    }
                }
            }

            // induction
            while (true)
            {
                // marked pairs in this induction step
                var marked = new List<TriangularPair<int>>();

                foreach (string successorLabel in GetLabels())
                {
                    //var markedPerInput = new List<TriangularPair<int>>();

                    foreach (var pair in undistinguishablePairs)
                    {
                        // Make function with input loop
                        // IsInductionDistinguishable

                        int p = Trans[pair.Fst][successorLabel];
                        int q = Trans[pair.Snd][successorLabel];
                        TriangularPair<int> successorPair = new TriangularPair<int>(p, q);
                        // For all pairs still undecided if for any successor label/input/suffix
                        // the pair goes into a pair (p, q) where the states are distinguishable
                        // for any suffix (that is already marked) we have to mark the pair.
                        // NOTE: BAD that we have to test p not equal to q (use marked table instead)
                        if (p != q && !undistinguishablePairs.Contains(successorPair)) // successor pair is marked
                        {
                            // re-add the pair because we cannot make
                            // p and q distinguishable for any suffix string
                            //markedPerInput.Add(pair);
                            marked.Add(pair);
                            //break; // next pair
                        }
                    }

                    //Debug.WriteLine($"Step {step}, Input = {successorLabel}, Count = {markedPerInput.Count}: " +
                    //                markedPerInput.Select(p =>
                    //                        $"({_renamer.ToDfaStateString(p.Fst)}, {_renamer.ToDfaStateString(p.Snd)})")
                    //                    .ToSetNotation());

                    // TODO: duplicated here
                    //foreach (var pair in markedPerInput)
                    //{
                    //    marked.Add(pair);
                    //}
                }

                foreach (var pair in marked)
                {
                    undistinguishablePairs.Remove(pair); // no-op for duplicates
                }

                if (marked.Count == 0)
                {
                    break; // no more distinguishable pairs have been removed
                }
            }

            // the following pairs are equivalent
            return undistinguishablePairs.ToArray();
        }

        /// <summary>
        /// Return true iff exactly one of the two states is final (i.e. one accepts the other does not).
        /// </summary>
        bool IsBasisDistinguishable(int p, int r)
        {
            bool marked;
            if (Accept.Contains(p))
            {
                marked = !Accept.Contains(r);
            }
            else
            {
                marked = Accept.Contains(r);
            }
            return marked;
        }

        IEnumerable<string> GetLabels()
        {
            // A hack, because the model is wrong
            return new Set<string>(Trans.Values.SelectMany(dict => dict.Keys));
        }

        class CharStateRenamer : IDfaStateRenamer
        {
            public string ToDfaStateString(int dfaStateIndex)
            {
                return new string((char) dfaStateIndex, 1);
            }
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

    /// <summary>
    /// Pair/tuple that by convention has a smaller fst value than snd value.
    /// </summary>
    public struct TriangularPair<T> : IEquatable<TriangularPair<T>> where T : IEquatable<T>, IComparable<T>
    {
        public T Fst;
        public T Snd;

        public TriangularPair(T p, T q)
        {
            if (p.CompareTo(q) < 0)
            {
                Fst = p; Snd = q;
            }
            else
            {
                Fst = q; Snd = p;
            }
        }

        public override string ToString()
        {
            return $"({Fst}, {Snd})";
        }

        public bool Equals(TriangularPair<T> other)
        {
            return EqualityComparer<T>.Default.Equals(Fst, other.Fst) &&
                   EqualityComparer<T>.Default.Equals(Snd, other.Snd);
        }

        public override bool Equals(object obj)
        {
            return obj is TriangularPair<T> other && Equals(other);
        }

        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode()
        {
            unchecked
            {
                return (EqualityComparer<T>.Default.GetHashCode(Fst) * 397) ^ EqualityComparer<T>.Default.GetHashCode(Snd);
            }
        }
    }
}
