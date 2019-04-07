using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using AutomataLib;
using Shouldly;
using Xunit;

namespace UnitTests
{
    public class AutomataTests
    {
        public struct Transition<TState>
        {
            public readonly TState SourceState;
            public readonly TState TargetState;
            public readonly char Label;

            public Transition(TState sourceState, char label, TState targetState)
            {
                SourceState = sourceState;
                Label = label;
                TargetState = targetState;
            }
        }

        // TODO: Should we allow states to be generic or just integers. Yes because of possibility of sparse (non-contigious) states

        // ASCII (7 bit) or ISO 8859-1 (8 bit)
        // First 128 characters of UTF-8/UTF-16(C# character, Java character)/UTF-32 are identical to ASCII
        // NOTE: We use US-ASCII here (first 128 characters 0..127, where 0 = zero = NULL = 'ε').

        // A non-deterministic finite-state automaton A = (Q, Σ, δ, q0, F) consists of a finite set of
        // states Q = {q1, ..., qn}, a finite set of input symbols or alphabet Σ = {a1, ..., ak}, a set
        // of transitions δ = {τ1, ..., τm} ⊆ Q × Σ × Q, the initial state q0 and the subset of accepting
        // states F ⊆ Q. For every (p, a) ∈ Q × Σ, δ(p, a) represents the subset of states
        //        δ(p, a) = {q ∈ Q : (p, a, q) ∈ δ}.
        // The number of states in the NFA is |Q| = n, the size of the alphabet is |Σ| = k and the number of
        // defined transitions is |δ| = m.
        //
        // DFA can be regarded as particular case of NFA where all sets δ(p, a) contain a single state,
        // and then m = kn.
        //      DFA is trimmed if |δ(p, a)| ≤ 1 for all (p, a) ∈ Q × Σ
        //      DFA is complete if |δ(p, a)| = 1 for all (p, a) ∈ Q × Σ
        // We normally trim a DFA because of single error state. In a trimmed DFA we allow some transitions
        // to be undefined in the DFA. In A 2D-table driven design, it is better just to define a complete DFA,
        // and make default(int) = 0 (zero) be the error state. When showing the digraph represenation of the DFA
        // all transitions to this state should not be shown

        /// <summary>
        /// Table-Driven Dfa with full/complete transition table as 2D array
        /// with k = n * m transition entries, where |Q| = n, and |Σ| = m.
        /// </summary>
        public class DfaTableDriven<TState> : IFiniteAutomata
            where TState : IEquatable<TState>
        {
            // Requires alphabet is a (sub)range of US-ASCII codes (127)....we cannot translate in lexer

            private readonly TState[] _stateName; // one-way translation should be sufficient

            // internal state machine based on int transitions (int source, int label, int target)
            // transformed to efficient 2DArray (table based transitions)
            private readonly int _maxState;
            private readonly char _minAscii;    // TODO: In reality all 128 entries are used (but binary alphabet, alphanumeric)
            private readonly char _maxAscii;    // TODO: In reality all 128 entries are used
            private readonly int[,] _nextState;   // jagged array vs 2DArray...we use 2DArray
            private readonly HashSet<int> _acceptStates;

            public DfaTableDriven(
                IEnumerable<TState> states, // should be unique...we do not test this here
                IEnumerable<char> alphabet, // should be unique...we do not test this here
                IEnumerable<Transition<TState>> transitions,
                TState startState,
                IEnumerable<TState> acceptingStates)
            {
                _stateName = states.ToArray();
                _maxState = _stateName.Length - 1;

                char minAscii = char.MaxValue, maxAscii = char.MinValue;
                foreach (char c in alphabet)
                {
                    if (c < minAscii) minAscii = c;
                    if (c > maxAscii) maxAscii = c;
                }

                if (minAscii == char.MinValue)
                {
                    throw new ArgumentException("Empty alphabet is not supported.");
                }
                if (_maxAscii > 127)
                {
                    throw new ArgumentException("Only US-ASCII is supported.");
                }

                int alphabetSize = maxAscii - minAscii + 1;

                _minAscii = minAscii;
                _maxAscii = maxAscii;
                _nextState = new int[_stateName.Length, alphabetSize];

                Start = Array.IndexOf(_stateName, startState);
                if (Start < 0)
                {
                    throw new ArgumentException($"The start state '{startState}' is not contained in the set of states.");
                }

                _acceptStates = new HashSet<int>();
                foreach (TState acceptingState in acceptingStates)
                {
                    int accept = Array.IndexOf(_stateName, acceptingState);
                    if (accept < 0)
                    {
                        throw new ArgumentException($"The accept state '{acceptingState}' is not contained in the set of states.");
                    }
                    _acceptStates.Add(accept);
                }

                var hash = new Dictionary<TState, int>();
                for (int i = 0; i < _stateName.Length; i++)
                {
                    hash.Add(_stateName[i], i);
                }

                if (hash.Count != _stateName.Length)
                {
                    throw new ArgumentException("States must have unique names");
                }

                // Translation is done on construction
                foreach (var move in transitions)
                {
                    int source = hash[move.SourceState];
                    int target = hash[move.TargetState];
                    _nextState[source, move.Label - _minAscii] = target;
                }
            }

            public int AlphabetSize => _maxAscii - _minAscii + 1;

            public int StateSize => _maxState + 1;

            public int TableSize => StateSize * AlphabetSize;

            public int GetTrimmedTableSize()
            {
                int size = 0;
                for (int s = 0; s < _nextState.GetLength(0); s += 1)
                {
                    for (int c = 0; c < _nextState.GetLength(1); c += 1)
                    {
                        int next = _nextState[s, c];
                        if (next != 0)
                        {
                            size += 1;
                        }
                    }
                }
                return size;
            }

            // Recognize
            // Accept
            public bool Match(string text)
            {
                int s = Start;
                foreach (char c in Letterizer<char>.Default.GetLetters(text))
                {
                    s = _nextState[s, c - _minAscii];
                }
                return _acceptStates.Contains(s);
            }

            public string PrintTable()
            {
                // TODO
                return null;
            }

            public IEnumerable<int> GetStates()
            {
                // We do not show the error state
                return Enumerable.Range(1, _maxState);
            }

            public int Start { get; }

            public IEnumerable<int> GetAcceptingStates()
            {
                return _acceptStates;
            }

            public IEnumerable<Transition<int>> GetTransitions()
            {
                // we do not show the error state!!!
                for (int s = 1; s < _nextState.GetLength(0); s += 1)
                {
                    for (int c = 0; c < _nextState.GetLength(1); c += 1)
                    {
                        int next = _nextState[s, c];
                        if (next != 0) // exclude error state
                        {
                            yield return new Transition<int>(s, (char)(c + _minAscii), next);
                        }
                    }
                }
            }
        }

        public interface IFiniteAutomata
        {
            IEnumerable<int> GetStates();

            //IEnumerable<char> GetAlphabet();

            int Start { get; }

            IEnumerable<int> GetAcceptingStates();

            IEnumerable<Transition<int>> GetTransitions();
        }

        public static class DotLanguagePrinter
        {
            public static string ToDotLanguage(IFiniteAutomata fa,
                DotRankDirection direction = DotRankDirection.LeftRight)
            {
                // TODO: We must convert states to integers, and use aliasing

                var sb = new StringBuilder();

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
                sb.AppendLine("n999999 [style=invis];"); // Invisible start node
                sb.AppendLine("n999999 -> n" + fa.Start); // Edge into start state

                // label states (overriding default n0, n1 names)
                foreach (int state in fa.GetStates())
                {
                    sb.AppendLine("n" + state + " [label=\"" + state + "\"]"); // TODO: GetStateLabel
                }

                // accept states are double circles
                foreach (int state in fa.GetAcceptingStates())
                {
                    sb.AppendLine("n" + state + " [peripheries=2];");
                }

                // nodes and edges are defined by transitions
                foreach (var t in fa.GetTransitions())
                {
                    sb.AppendLine("n" + t.SourceState + " -> n" + t.TargetState + " [label=\"" + t.Label + "\"];");
                }

                sb.AppendLine("}");

                return sb.ToString();
            }
        }

        [Fact]
        public void Test()
        {
            // Language = {"ace", "add", "bad" "bed", "bee", "cab", "dad"}
            // (and each must be terminated by EOF).
            // TODO: There is no EOF character in C#
            // TODO: '$' has ASCII value 36
            // TODO: a..e has ASCII values 97..100
            var dfaTrie = new DfaTableDriven<int>(
                Enumerable.Range(0, 20), // 0..19
                SetOf('a', 'b', 'c', 'd', 'e','$'),
                new[]
                {
                    // state 0 is error state
                    new Transition<int>(1, 'a', 2),
                    new Transition<int>(1, 'b', 3),
                    new Transition<int>(1, 'c', 4),
                    new Transition<int>(1, 'd', 5),
                    new Transition<int>(2, 'c', 16),
                    new Transition<int>(2, 'd', 17),
                    new Transition<int>(3, 'a', 11),
                    new Transition<int>(3, 'e', 12),
                    new Transition<int>(4, 'a', 9),
                    new Transition<int>(5, 'a', 6),
                    new Transition<int>(6, 'd', 7),
                    new Transition<int>(7, '$', 8),
                    // state 8 is accepting state
                    new Transition<int>(9, 'b', 10),
                    new Transition<int>(10, '$', 8),
                    new Transition<int>(11, 'd', 15),
                    new Transition<int>(12, 'd', 13),
                    new Transition<int>(12, 'e', 14),
                    new Transition<int>(13, '$', 8),
                    new Transition<int>(14, '$', 8),
                    new Transition<int>(15, '$', 8),
                    new Transition<int>(16, 'e', 19),
                    new Transition<int>(17, 'd', 18),
                    new Transition<int>(18, '$', 8),
                    new Transition<int>(19, '$', 8),
                },
                startState: 1,
                acceptingStates: SetOf(8));

            // sparse transition table that would benefit from compression
            dfaTrie.AlphabetSize.ShouldBe(101 - 36 + 1);
            dfaTrie.StateSize.ShouldBe(20);
            dfaTrie.TableSize.ShouldBe(1320); // 20 * 66

            // Only 24 out of 1320 cells in the transition table are actually used to simulate the DFA
            dfaTrie.GetTrimmedTableSize().ShouldBe(24);

            // http://viz-js.com/
            SaveFile("trie.dot", DotLanguagePrinter.ToDotLanguage(dfaTrie));
        }

        private IEnumerable<T> SetOf<T>(params T[] set)
        {
            return set;
        }

        private static void SaveFile(string filename, string contents)
        {
            File.WriteAllText(GetPath(filename), contents);
        }

        private static string GetPath(string filename)
        {
            string artifactsPath = GetArtifactsPath();
            Directory.CreateDirectory(artifactsPath);
            string path = Path.Combine(artifactsPath, filename);
            return path;
        }

        private static string GetArtifactsPath()
        {
            string path = new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath;
            while (true)
            {
                path = Path.GetDirectoryName(path);
                if (Directory.GetDirectories(path, ".git", SearchOption.TopDirectoryOnly).Length == 1)
                {
                    break;
                }
            }

            string artifactsPath = Path.Combine(path, "artifacts");
            return artifactsPath;
        }
    }
}
