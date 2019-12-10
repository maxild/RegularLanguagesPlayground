using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AutomataLib;

namespace FiniteAutomata
{
    // TODO: ToRegex: Convert Dfa to Regex
    // TODO: Convert two DFAs to their product DFA (Intersection, Difference of regular languages are regular languages)

    // Note: Keys of dictionary transitions are sorted such that states are ordered 0,1,2,3,4 (or whatever)

    /// <summary>
    /// A deterministic finite automaton (DFA).
    /// </summary>
    public class Dfa<TAlphabet> : IDeterministicFiniteAutomaton<TAlphabet, int>, IFiniteAutomatonStateHomomorphism<int>
        where TAlphabet : IEquatable<TAlphabet>
    {
        class CharStateRenamer : IDfaStateRenamer
        {
            public string ToDfaStateString(int dfaStateIndex)
            {
                return new string((char) dfaStateIndex, 1);
            }
        }

        private readonly IDfaStateRenamer _renamer;
        private readonly HashSet<int> _acceptStates;
        private readonly IDictionary<int, Dictionary<TAlphabet, int>> _delta;

        public Dfa(
            int startState,
            IEnumerable<int> acceptStates,
            IDictionary<int, Dictionary<TAlphabet, int>> delta,
            IDfaStateRenamer renamer)
        {
            StartState = startState;
            _acceptStates = new HashSet<int>(acceptStates);
            _delta = delta;
            _renamer = renamer;
        }

        /// <summary>
        /// Naming states with ASCII letters and calling AddTrans to built up the states and transitions
        /// </summary>
        public Dfa(
            char startState,
            IEnumerable<char> acceptStates)
        {
            StartState = startState;
            _acceptStates = new HashSet<int>(acceptStates.Select(x => (int)x));
            _delta = new SortedDictionary<int, Dictionary<TAlphabet, int>>(); // keys, states are sorted
            _renamer = new CharStateRenamer();
        }

        public int StartState { get; }

        public bool IsAcceptState(int state)
        {
            return _acceptStates.Contains(state);
        }

        public IEnumerable<int> GetStates()
        {
            return _delta.Keys;
        }

        public IEnumerable<int> GetTrimmedStates()
        {
            return GetStates();
        }

        public IEnumerable<TAlphabet> GetAlphabet()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<int> GetAcceptStates()
        {
            return _acceptStates;
        }

        public IEnumerable<Transition<TAlphabet, int>> GetTransitions()
        {
            return from kvp in _delta
                let sourceState = kvp.Key
                from pair in kvp.Value
                let label = pair.Key
                let targetState = pair.Value
                select Transition.Move(sourceState, label, targetState);
        }

        public IEnumerable<Transition<TAlphabet, int>> GetTrimmedTransitions()
        {
            return GetTransitions();
        }

        public int TransitionFunction(int state, IEnumerable<TAlphabet> input)
        {
            int s = state;
            foreach (TAlphabet letter in input)
            {
                if (_delta[s].TryGetValue(letter, out int nextState))
                {
                    s = nextState;
                }
                else
                {
                    // Sparse model has to have a fixed dead state
                    return -1; // dead state
                }
            }

            return s;
        }

        public string GetStateLabel(int state, string sep)
        {
            return _renamer.ToDfaStateString(state);
        }

        public bool IsMatch(IEnumerable<TAlphabet> input)
        {
            int s = TransitionFunction(StartState, input);
            return s != -1 && _acceptStates.Contains(s);
        }

        public void AddTrans(int s1, TAlphabet label, int s2)
        {
            Dictionary<TAlphabet, int> transitions;
            if (_delta.ContainsKey(s1))
            {
                transitions = _delta[s1];
            }
            else
            {
                transitions = new Dictionary<TAlphabet, int>();
                _delta.Add(s1, transitions);
            }

            if (!_delta.ContainsKey(s2))
            {
                _delta.Add(s2, new Dictionary<TAlphabet, int>());
            }

            transitions.Add(label, s2); // Note: Will throw ArgumentException if key all ready exists
        }

        // For debugging
        public string DisplayEquivalentPairs()
        {
            return GetEquivalentPairs()
                .Select(p => new TriangularPair<string>(_renamer.ToDfaStateString(p.Fst), _renamer.ToDfaStateString(p.Snd)))
                .ToVectorString();
        }

        // TODO: Uses non-reachable states (inefficient)
        TriangularPair<int>[] GetEquivalentPairs()
        {
            // table filling algorithm:

            var undistinguishablePairs = new HashSet<TriangularPair<int>>(); // not marked!!!

            // basis
            foreach (int p in _delta.Keys)
            {
                foreach (int q in _delta.Keys)
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
                        if (_delta[pair.Fst].TryGetValue(successorLabel, out var p) &&
                            _delta[pair.Snd].TryGetValue(successorLabel, out var q))
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
                    .Select(stateIndex => _renamer.ToDfaStateString(stateIndex)).ToVectorString())
                .ToVectorString();
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
            var visited = new Set<int>(new[] {StartState});

            var worklist = new Queue<int>();
            worklist.Enqueue(StartState);

            while (worklist.Count > 0)
            {
                int state = worklist.Dequeue();
                foreach (var label in GetLabels())
                {
                    // Trans indeholder dead states, og er ikke defineret for alle labels,
                    // og derfor benytter vi TryGetValue
                    if (_delta[state].TryGetValue(label, out var toState) && !visited.Contains(toState))
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
                ? new Set<int>(_delta.Keys)
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
                if (blockState.Contains(StartState))
                {
                    startBlockState = blockState;
                }
                if (blockState.Any(s => _acceptStates.Contains(s)))
                {
                    acceptBlocks.Add(blockState);
                }

                IDictionary<TAlphabet, Set<int>> transition = new Dictionary<TAlphabet, Set<int>>();

                foreach (var label in GetLabels())
                {
                    //int toState = Trans[blockState.First()][label];
                    if (_delta[blockState.First()].TryGetValue(label, out var toState))
                    {
                        Set<int> toBlockState = blockStates.First(block => block.Contains(toState));
                        transition.Add(label, toBlockState);
                    }
                }

                blockTrans.Add(blockState, transition);
            }

            var renamer = new MinimizedDfaRenamer(blockStates, _renamer);

            var minDfaTrans = Rename(renamer, blockTrans);

            int minDfaStartState = renamer.ToDfaStateIndex(startBlockState);

            Set<int> minDfaAcceptStates = AcceptStates(blockStates, renamer, acceptBlocks);

            return new Dfa<TAlphabet>(minDfaStartState, minDfaAcceptStates, minDfaTrans, renamer);
        }

        static IDictionary<int, Dictionary<TAlphabet, int>> Rename(
            MinimizedDfaRenamer renamer,
            IDictionary<Set<int>, IDictionary<TAlphabet, Set<int>>> trans)
        {
            var newDfaTrans = new SortedDictionary<int, Dictionary<TAlphabet, int>>(); // keys/states are sorted

            foreach (KeyValuePair<Set<int>, IDictionary<TAlphabet, Set<int>>> entry in trans)
            {
                Set<int> blockState = entry.Key;
                var newDfaTransRow = new Dictionary<TAlphabet, int>();
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
            if (_acceptStates.Contains(p))
            {
                marked = !_acceptStates.Contains(r);
            }
            else
            {
                marked = _acceptStates.Contains(r);
            }
            return marked;
        }

        IEnumerable<TAlphabet> GetLabels()
        {
            // A hack, because the model is wrong
            return new Set<TAlphabet>(_delta.Values.SelectMany(dict => dict.Keys));
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
            return _dfaStateToBlockState[dfaStateIndex].Select(s => _renamer.ToDfaStateString(s)).ToVectorString();
        }
    }
}
