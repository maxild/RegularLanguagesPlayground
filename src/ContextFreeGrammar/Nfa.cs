using System;
using System.Collections.Generic;
using System.Text;
using AutomataLib;

namespace ContextFreeGrammar
{
    /// <summary>
    /// Non-deterministic Finite Automaton (Q, Σ, delta, q(0), F) from Automata Theory (with
    /// possible ε-transitions, aka ε-moves).
    /// </summary>
    /// <typeparam name="TState"></typeparam>
    /// <typeparam name="TAlphabet"></typeparam>
    public class Nfa<TState, TAlphabet>
        where TAlphabet : IEquatable<TAlphabet>
        where TState : IEquatable<TState>, INumberedItem
    {
        private static readonly ISet<TState> s_deadState = new HashSet<TState>();

        // Adjacency ('sparse') list representation of digraph with labeled edges (moves, transitions)
        //
        //      _delta: array/list of adjacency list of moves/transitions
        //
        // We can only use (outer)
        //
        //private readonly Dictionary<TState, List<Move<TAlphabet>>> _delta;                // Automaton<TAlphabet> uses this one
        //private readonly Dictionary<TState, List<Transition<TAlphabet>>> _delta;          // Nfa<TAlphabet> uses this one
        private readonly Dictionary<TState, IDictionary<TAlphabet, ISet<TState>>> _delta;

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
            _delta = new Dictionary<TState, IDictionary<TAlphabet, ISet<TState>>>();
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
                    foreach (var transOfFromState in _delta)
                    {
                        states.Add(transOfFromState.Key);
                        foreach (KeyValuePair<TAlphabet, ISet<TState>> transitions in transOfFromState.Value)
                        {
                            states.UnionWith(transitions.Value);
                        }
                    }

                    _states = states;
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
                    foreach (IDictionary<TAlphabet, ISet<TState>> transOfSomeFromState in _delta.Values)
                    {
                        alphabet.UnionWith(transOfSomeFromState.Keys);
                    }

                    _alphabet = alphabet;
                    _alphabetVersion = _version;
                }

                return _alphabet;
            }
        }

        public void AddTransition(TState from, TAlphabet label, TState to)
        {
            IDictionary<TAlphabet, ISet<TState>> transOfFromState;
            if (_delta.ContainsKey(from))
            {
                transOfFromState = _delta[from];
            }
            else
            {
                transOfFromState = new Dictionary<TAlphabet, ISet<TState>>();
                _delta.Add(from, transOfFromState);
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
            if (_delta.TryGetValue(state, out var transOfFromState) && transOfFromState.TryGetValue(label, out var toStates))
            {
                return toStates;
            }
            return s_deadState;
        }

        public DeterministicAutomata<TAlphabet> SubsetConstruction()
        {
            // int vs ProductionItemSet...renaming of states, and ToDotLanguage with state descriptions
            return null;
        }

        // Web pages for drawing with dot
        // http://viz-js.com/                       ( The BEST)
        // https://graphs.grevian.org/graph
        // http://www.webgraphviz.com/              (NOT good)
        // Tools for drawing with dot in the browser
        // https://github.com/magjac/d3-graphviz
        public string ToDotLanguage(DotRankDirection direction = DotRankDirection.LeftRight)
        {
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
            sb.AppendLine("n999999 [style=invis];");            // Invisible start node
            sb.AppendLine("n999999 -> " + StartState.Id + ";"); // Edge into start state

            // label states (overriding default n0, n1 names)
            foreach (TState state in States)
            {
                sb.AppendLine(state.Id + " [label=\"" + state.Label + "\"];");
            }

            // accept states are double circles
            foreach (TState state in States)
            {
                if (AcceptingStates.Contains(state))
                    sb.AppendLine(state.Id + " [peripheries=2];");
            }

            // nodes and edges are defined by transitions
            foreach (KeyValuePair<TState, IDictionary<TAlphabet, ISet<TState>>> entry in _delta)
            {
                TState fromState = entry.Key;
                foreach (KeyValuePair<TAlphabet, ISet<TState>> transOfFromState in entry.Value)
                {
                    TAlphabet label = transOfFromState.Key;
                    foreach (TState toState in transOfFromState.Value)
                    {
                        sb.AppendLine(fromState.Id + " -> " + toState.Id + " [label=\"" + label + "\"];");
                    }
                }
            }

            sb.AppendLine("}");

            return sb.ToString();
        }
    }

    // Online Learning Tools
    // https://aude.imag.fr/aude/
    // http://automatatutor.com/about/  (http://pages.cs.wisc.edu/~loris/teaching.html)

    ///// <summary>
    ///// Basic Automata
    ///// </summary>
    ///// <typeparam name="TAlphabet"></typeparam>
    //public class Automata<TAlphabet>
    //{
    //    private Dictionary<int, List<Move<TAlphabet>>> _delta;
    //    // states are always integers
    //    private int _startState;
    //    private HashSet<int> _acceptingStates;
    //    private int _maxState;
    //}
}
