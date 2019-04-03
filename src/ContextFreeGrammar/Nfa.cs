using System;
using System.Collections.Generic;
using System.Text;

namespace ContextFreeGrammar
{
    /// <summary>
    /// (Q, Σ, delta, q(0), F) from Automata Theory.
    /// </summary>
    /// <typeparam name="TState"></typeparam>
    /// <typeparam name="TAlphabet"></typeparam>
    public class Nfa<TState, TAlphabet>
        where TAlphabet : IEquatable<TAlphabet>
        where TState : IEquatable<TState>
    {
        private static readonly ISet<TState> DEAD_STATE = new HashSet<TState>();
        private IDictionary<TState, IDictionary<TAlphabet, ISet<TState>>> _trans;

        private int _version;
        private ISet<TState> _states;
        private int _statesVersion;
        private ISet<TAlphabet> _alphabet;
        private int _alphabetVersion;

        public Nfa(TState startState)
        {
            StartState = startState;
            _states = new HashSet<TState> { startState};
            _alphabet = new HashSet<TAlphabet>();
            AcceptingStates = new HashSet<TState>();
            _trans = new Dictionary<TState, IDictionary<TAlphabet, ISet<TState>>>();
        }

        /// <summary>
        /// q(0)
        /// </summary>
        public TState StartState { get; }

        /// <summary>
        /// F
        /// </summary>
        public ISet<TState> AcceptingStates { get; }

        /// <summary>
        /// Q
        /// </summary>
        public ISet<TState> States
        {
            get
            {
                if (_statesVersion != _version)
                {
                    var states = new HashSet<TState>();
                    states.Add(StartState);
                    foreach (var transOfFromState in _trans)
                    {
                        states.Add(transOfFromState.Key);
                        foreach (KeyValuePair<TAlphabet, ISet<TState>> transitions in transOfFromState.Value)
                        {
                            states.UnionWith(transitions.Value);
                        }
                    }
                    _statesVersion = _version;
                }

                return _states;
            }
        }

        /// <summary>
        /// Σ
        /// </summary>
        public ISet<TAlphabet> Alphabet
        {
            get
            {
                if (_alphabetVersion != _version)
                {
                    var alphabet = new HashSet<TAlphabet>();
                    foreach (IDictionary<TAlphabet, ISet<TState>> transOfSomeFromState in _trans.Values)
                    {
                        alphabet.UnionWith(transOfSomeFromState.Keys);
                    }
                    _alphabetVersion = _version;
                }

                return _alphabet;
            }
        }

        public void AddTransition(TState from, TAlphabet label, TState to)
        {
            IDictionary<TAlphabet, ISet<TState>> transOfFromState;
            if (_trans.ContainsKey(from))
            {
                transOfFromState = _trans[from];
            }
            else
            {
                transOfFromState = new Dictionary<TAlphabet, ISet<TState>>();
                _trans.Add(from, transOfFromState);
            }

            ISet<TState> fromStates;
            if (transOfFromState.ContainsKey(label))
            {
                fromStates = transOfFromState[label];
            }
            else
            {
                fromStates = new HashSet<TState>();
                transOfFromState.Add(label, fromStates);
            }

            fromStates.Add(to);

            _version += 1;
        }

        /// <summary>
        /// delta
        /// </summary>
        public ISet<TState> Delta(TState state, TAlphabet label)
        {
            if (_trans.TryGetValue(state, out var transOfFromState) && transOfFromState.TryGetValue(label, out var toStates))
            {
                return toStates;
            }
            return DEAD_STATE;
        }

        public void ToDotFile()
        {
            // TODO: We must convert states to integers, and use aliasing

            var sb = new StringBuilder();

            sb.AppendLine("digraph dfa {");
            sb.AppendLine("size=\"11,8.25\";");
            //sb.AppendLine("rotate=90;");
            sb.AppendLine("rankdir=LR;");
            sb.AppendLine("n999999 [style=invis];"); // Invisible start node
            sb.AppendLine("n999999 -> n" + StartState);   // Edge into start state

            // labels that indicate the NFA states of the subset construction
            // TODO: We must convert states to integers, and use aliasing
            //foreach (TState state in States)
            //{
            //    sb.AppendLine("n" + state + " [label=\"" + _renamer.ToDfaStateString(state) + "\"]");
            //}

            // Accept states are double circles
            foreach (TState state in States)
            {
                if (AcceptingStates.Contains(state))
                    sb.AppendLine("n" + state + " [peripheries=2];");
            }

            // The transitions
            foreach (KeyValuePair<TState, IDictionary<TAlphabet, ISet<TState>>> entry in _trans)
            {
                TState fromState = entry.Key;
                foreach (KeyValuePair<TAlphabet, ISet<TState>> transOfFromState in entry.Value)
                {
                    TAlphabet label = transOfFromState.Key;
                    foreach (TState toState in transOfFromState.Value)
                    {
                        sb.AppendLine("n" + fromState + " -> n" + toState + " [label=\"" + label + "\"];");
                    }
                }
            }

            sb.AppendLine("}");
        }
    }
}
