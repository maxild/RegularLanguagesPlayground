using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AutomataLib;

namespace FiniteAutomata
{
    // TODO: ToRegex: Convert Dfa to Regex
    // TODO: Convert two DFAs to their product DFA (Intersection, Difference of regular languages are regular languages)

    // Note: Keys of dictionary transitions are sorted such that states are ordered 0,1,2,3,4 (or whatever)

    /// <summary>
    /// A deterministic finite automaton (DFA) is represented as a Map
    /// from state number (int) to a Map from label (a String,
    /// non-null) to a target state (an int).
    /// </summary>
    public class Dfa<TAlphabet>
        where TAlphabet : IEquatable<TAlphabet>
    {
        private readonly IDfaStateRenamer _renamer;

        public Dfa(
            int startState,
            Set<int> acceptStates,
            IDictionary<int, IDictionary<TAlphabet, int>> trans,
            IDfaStateRenamer renamer)
        {
            Start = startState;
            Accept = acceptStates;
            Trans = trans;
            _renamer = renamer;
        }

        /// <summary>
        /// Naming states with ASCII letters and calling AddTrans to built up the states and transitions
        /// </summary>
        public Dfa(
            char startState,
            IEnumerable<char> acceptingStates)
        {
            Start = startState;
            Accept = new Set<int>(acceptingStates.Select(x => (int)x));
            Trans = new SortedDictionary<int, IDictionary<TAlphabet, int>>(); // keys, states are sorted
            _renamer = new CharStateRenamer();
        }

        public int Start { get; }

        public Set<int> Accept { get; }

        IDictionary<int, IDictionary<TAlphabet, int>> Trans { get; }

        public void AddTrans(int s1, TAlphabet label, int s2)
        {
            IDictionary<TAlphabet, int> transitions;
            if (Trans.ContainsKey(s1))
            {
                transitions = Trans[s1];
            }
            else
            {
                transitions = new Dictionary<TAlphabet, int>();
                Trans.Add(s1, transitions);
            }
            transitions.Add(label, s2);
        }

        public override string ToString()
        {
            return $"DFA start = {_renamer.ToDfaStateString(Start)}, Accept = " +
                   Accept.Select(x => _renamer.ToDfaStateString(x)).ToSetNotation();
        }

        public bool Match(string s)
        {
            int state = Start;
            foreach (TAlphabet letter in Letterizer<TAlphabet>.Default.GetLetters(s))
            {
                if (Trans[state].TryGetValue(letter, out int newState))
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

        // TODO: Uses non-reachable states (inefficient)
        TriangularPair<int>[] GetEquivalentPairs()
        {
            // table filling algorithm:

            var undistinguishablePairs = new Set<TriangularPair<int>>(); // not marked!!!

            // basis
            foreach (int p in Trans.Keys)
            {
                foreach (int q in Trans.Keys)
                {
                    if (q <= p) continue; // triangular
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

                foreach (TAlphabet successorLabel in GetLabels())
                {
                    foreach (var pair in undistinguishablePairs)
                    {
                        // Make function with input loop
                        // IsInductionDistinguishable

                        //int p = Trans[pair.Fst][successorLabel];
                        //int q = Trans[pair.Snd][successorLabel];
                        if (Trans[pair.Fst].TryGetValue(successorLabel, out var p) &&
                            Trans[pair.Snd].TryGetValue(successorLabel, out var q))
                        {
                            TriangularPair<int> successorPair = new TriangularPair<int>(p, q);

                            // For all pairs still undecided if for any successor label/input/suffix
                            // the pair goes into a pair (p, q) where the states are distinguishable
                            // for any suffix (that is already marked) we have to mark the pair.

                            // NOTE: BAD that we have to test p not equal to q (use marked table instead)
                            if (p != q && !undistinguishablePairs.Contains(successorPair)) // successor pair is marked
                            {
                                marked.Add(pair);
                            }
                        }
                    }

                    //Debug.WriteLine($"Step {step}, Input = {successorLabel}, Count = {markedPerInput.Count}: " +
                    //                markedPerInput.Select(p =>
                    //                        $"({_renamer.ToDfaStateString(p.Fst)}, {_renamer.ToDfaStateString(p.Snd)})")
                    //                    .ToSetNotation());
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

        public string DisplayMergedEqSets()
        {
            TriangularPair<int>[] eqStatePairs = GetEquivalentPairs();
            Set<int>[] mergedEqSets = GetMergedEqSets(eqStatePairs);
            return mergedEqSets.Select(set =>set
                    .Select(stateIndex => _renamer.ToDfaStateString(stateIndex)).ToSetNotation())
                .ToSetNotation();
        }

        Set<int>[] GetMergedEqSets(TriangularPair<int>[] eqStatePairs)
        {
            BitArray indexIsAdded = new BitArray(eqStatePairs.Length);

            // Merge equivalent pairs to find disjoint state blocks with more than one equivalent states

            // naive inefficient algorithm...(n-1)*n/2 combinations
            List<Set<int>> listOfEqBlocks = new List<Set<int>>();
            for (int i = 0; i < eqStatePairs.Length; i++)
            {
                listOfEqBlocks.Add(new Set<int>());

                if (!indexIsAdded[i])
                {
                    listOfEqBlocks[i].Add(eqStatePairs[i].Fst);
                    listOfEqBlocks[i].Add(eqStatePairs[i].Snd);
                    indexIsAdded[i] = true;
                }

                for (int j = i + 1; j < eqStatePairs.Length; j++)
                {
                    if (eqStatePairs[j].IsEqToBlock(listOfEqBlocks[i]))
                    {
                        listOfEqBlocks[i].Add(eqStatePairs[j].Fst);
                        listOfEqBlocks[i].Add(eqStatePairs[j].Snd);
                        indexIsAdded[j] = true;
                    }
                }
            }

            return listOfEqBlocks.Where(block => block.Count > 0).ToArray();
        }

        Set<int> GetReachableStates()
        {
            // Breadth First Traversal
            var visited = new Set<int>(new[] {Start});

            var worklist = new Queue<int>();
            worklist.Enqueue(Start);

            while (worklist.Count > 0)
            {
                int state = worklist.Dequeue();
                foreach (var label in GetLabels())
                {
                    // Trans indeholder dead states, og er ikke defineret for alle labels,
                    // og derfor benytter vi TryGetValue
                    if (Trans[state].TryGetValue(label, out var toState) && !visited.Contains(toState))
                    {
                        worklist.Enqueue(toState);
                        visited.Add(toState);
                    }
                }
            }

            return visited;
        }

        public Dfa<TAlphabet> ToMinimumDfa(bool skipRemovalOfUnreachableStates = false)
        {
            // First eliminate any state(s) that cannot be reached from the start state
            var minimizedStates = skipRemovalOfUnreachableStates
                ? new Set<int>(Trans.Keys)
                : GetReachableStates();

            // Use the table filling algorithm to find all the pairs of equivalent states
            TriangularPair<int>[] eqStatePairs = GetEquivalentPairs();

            // Partition the set of states Q into blocks of mutually equivalent states
            Set<int>[] blocksWithEqStates = GetMergedEqSets(eqStatePairs);
            Set<int>[] blocksWithSingleState =
                minimizedStates.Difference(blocksWithEqStates).Select(state => new Set<int>(new []{state})).ToArray();

            List<Set<int>> blockStates = blocksWithEqStates.Concat(blocksWithSingleState).ToList();

            Set<int> startBlockState = null;
            List<Set<int>> acceptBlocks = new List<Set<int>>();

            // The transition relation of the DFA = (States, Transition)
            var blockTrans = new Dictionary<Set<int>, IDictionary<TAlphabet, Set<int>>>();

            foreach (var blockState in blockStates)
            {
                if (blockState.Contains(Start))
                {
                    startBlockState = blockState;
                }
                if (blockState.Any(s => Accept.Contains(s)))
                {
                    acceptBlocks.Add(blockState);
                }

                IDictionary<TAlphabet, Set<int>> transition = new Dictionary<TAlphabet, Set<int>>();

                foreach (var label in GetLabels())
                {
                    //int toState = Trans[blockState.First()][label];
                    if (Trans[blockState.First()].TryGetValue(label, out var toState))
                    {
                        Set<int> toBlockState = blockStates.First(block => block.Contains(toState));
                        transition.Add(label, toBlockState);
                    }
                }

                blockTrans.Add(blockState, transition);
            }

            var renamer = new MinimizedDfaRenamer(blockStates, _renamer);

            IDictionary<int, IDictionary<TAlphabet, int>> minDfaTrans = Rename(renamer, blockTrans);

            int minDfaStartState = renamer.ToDfaStateIndex(startBlockState);

            Set<int> minDfaAcceptStates = AcceptStates(blockStates, renamer, acceptBlocks);

            return new Dfa<TAlphabet>(minDfaStartState, minDfaAcceptStates, minDfaTrans, renamer);
        }

        static IDictionary<int, IDictionary<TAlphabet, int>> Rename(
            MinimizedDfaRenamer renamer,
            IDictionary<Set<int>, IDictionary<TAlphabet, Set<int>>> trans)
        {
            var newDfaTrans = new SortedDictionary<int, IDictionary<TAlphabet, int>>(); // keys/states are sorted

            foreach (KeyValuePair<Set<int>, IDictionary<TAlphabet, Set<int>>> entry in trans)
            {
                Set<int> blockState = entry.Key;
                IDictionary<TAlphabet, int> newDfaTransRow = new Dictionary<TAlphabet, int>();
                foreach (KeyValuePair<TAlphabet, Set<int>> tr in entry.Value)
                {
                    newDfaTransRow.Add(tr.Key, renamer.ToDfaStateIndex(tr.Value));
                }
                newDfaTrans.Add(renamer.ToDfaStateIndex(blockState), newDfaTransRow);
            }

            return newDfaTrans;
        }

        static Set<int> AcceptStates(
            IEnumerable<Set<int>> blockStates,
            MinimizedDfaRenamer renamer,
            List<Set<int>> acceptBlocks)
        {
            Set<int> acceptStates = new Set<int>();
            foreach (Set<int> blockState in blockStates)
            {
                if (acceptBlocks.Any(finalState => blockState.Equals(finalState)))
                    acceptStates.Add(renamer.ToDfaStateIndex(blockState));

            }
            return acceptStates;
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

        IEnumerable<TAlphabet> GetLabels()
        {
            // A hack, because the model is wrong
            return new Set<TAlphabet>(Trans.Values.SelectMany(dict => dict.Keys));
        }

        class CharStateRenamer : IDfaStateRenamer
        {
            public string ToDfaStateString(int dfaStateIndex)
            {
                return new string((char) dfaStateIndex, 1);
            }
        }

        public string ToDotLanguage(DotRankDirection direction = DotRankDirection.LeftRight)
        {
            var sb = new StringBuilder();
            sb.AppendLine("// Format this file as a Postscript file with ");
            sb.AppendLine($"//    dot -Tps [filename.dot] -o out.ps\n");
            sb.AppendLine("digraph dfa {");
            switch (direction)
            {
                    case DotRankDirection.TopBottom:
                        sb.AppendLine("size=\"8.25,11\"; /* A4 paper portrait: 8.27 × 11.69 inches */");
                        sb.AppendLine("rankdir=TB;");
                        break;
                    case DotRankDirection.LeftRight:
                        sb.AppendLine("size=\"11,8.25\"; /* A4 paper landscape: 11.69 x 8.27 inches */");
                        sb.AppendLine("rankdir=LR;");
                        break;
            }

            // start state arrow indicator
            sb.AppendLine("n999999 [style=invis];");    // Invisible start node
            sb.AppendLine("n999999 -> n" + Start);      // Edge into start state

            // label states (overriding default n0, n1 names)
            foreach (int state in Trans.Keys)
                sb.AppendLine("n" + state + " [label=\"" + _renamer.ToDfaStateString(state) + "\"]");

            // accept states are double circles
            foreach (int state in Trans.Keys)
                if (Accept.Contains(state))
                    sb.AppendLine("n" + state + " [peripheries=2];");

            // nodes and edges are defined by transitions
            foreach (KeyValuePair<int, IDictionary<TAlphabet, int>> entry in Trans)
            {
                int fromState = entry.Key;
                foreach (KeyValuePair<TAlphabet, int> s1Trans in entry.Value)
                {
                    TAlphabet label = s1Trans.Key;
                    int toState = s1Trans.Value;
                    sb.AppendLine("n" + fromState + " -> n" + toState + " [label=\"" + label + "\"];");
                }
            }

            sb.AppendLine("}");

            return sb.ToString();
        }
    }

    public class MinimizedDfaRenamer : IDfaStateRenamer
    {
        private readonly Dictionary<Set<int>, int> _blockStatesToDfaState;
        private readonly List<Set<int>> _dfaStateToBlockState;
        private readonly IDfaStateRenamer _renamer;

        public MinimizedDfaRenamer(ICollection<Set<int>> blockStates, IDfaStateRenamer renamer)
        {
            _blockStatesToDfaState = new Dictionary<Set<int>, int>(blockStates.Count);
            _dfaStateToBlockState = new List<Set<int>>(blockStates.Count);
            int count = 0;
            foreach (Set<int> k in blockStates)
            {
                int nfaStateIndex = count;
                _blockStatesToDfaState.Add(k, nfaStateIndex);
                _dfaStateToBlockState.Add(k);
                count += 1;
            }

            _renamer = renamer;
        }

        public int ToDfaStateIndex(Set<int> blockState)
        {
            return _blockStatesToDfaState[blockState];
        }

        public string ToDfaStateString(int dfaStateIndex)
        {
            return _dfaStateToBlockState[dfaStateIndex].Select(s => _renamer.ToDfaStateString(s)).ToSetNotation();
        }
    }
}
