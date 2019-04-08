using System.Collections.Generic;

namespace ContextFreeGrammar
{
    /// <summary>
    /// Deterministic Finite Automaton (Q, Σ, delta, q(0), F) from Automata Theory.
    /// </summary>
    /// <typeparam name="TAlphabet"></typeparam>
    public class DeterministicAutomata<TAlphabet>
    {
        private readonly Dictionary<int, Dictionary<TAlphabet, int>> _delta;
        private int _version;
        private ISet<TAlphabet> _alphabet;
        private int _alphabetVersion;

        public DeterministicAutomata()
        {
            // TODO
            _version = 0;
            _delta = new Dictionary<int, Dictionary<TAlphabet, int>>();
        }

        /// <summary>
        /// q(0)
        /// </summary>
        public int StartState { get; }

        /// <summary>
        /// F
        /// </summary>
        public IEnumerable<int> AcceptingStates { get; }

        /// <summary>
        /// Q
        /// </summary>
        public IEnumerable<int> States => _delta.Keys;

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
                    foreach (var transOfSomeFromState in _delta.Values)
                    {
                        alphabet.UnionWith(transOfSomeFromState.Keys);
                    }

                    _alphabet = alphabet;
                    _alphabetVersion = _version;
                }

                return _alphabet;
            }
        }
    }
}
