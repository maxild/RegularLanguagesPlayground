using System.Collections.Generic;
using AutomataLib;

namespace AutomataRepresentations
{
    // Sources:
    // Dragon book pp pp. 144-146 (1st ed), pp 184-186 (2nd edition)
    // https://compilers.iecc.com/comparch/article/94-10-131
    // https://stackoverflow.com/questions/29139162/dfa-state-transition-table-compression
    // https://linux.thai.net/~thep/datrie/
    // https://anoopsarkar.github.io/compilers-class/assets/lectures/lex9-implementing-dfas.pdf
    // PROBLEM: I still don't understand how to build the base-next-check-default arrays!!!
    public class DfaCompressedTable<TState>
    {
        private readonly int[] _base; // parallel to states
        // we want make the next/check arrays as short as possible
        // by taking advantage of similarities between states (i..e.
        // rows in the normal transition table

        // So, the "next" array is nothing but a linear representation of the adjacency matrix with
        // some of the entries overlapped (hence table compression)
        private readonly int[] _next;
        private readonly int[] _check;
        private readonly int[] _default; // many rows look alike, this will mimic a lot of row data, and make next/check small. default array is use to give n alternative base/offset into next/check arrays.

        // NOTE: The transition table rows are allocated in overlapping manner, allowing
        // the free cells to be used by other rows.

        // NOTE: In a Lexer another array would reference the pattern that was matched (if any)
        // such that user actions can be invoked

        public DfaCompressedTable(
            IEnumerable<TState> states, // should be unique...we do not test this here
            IEnumerable<char> alphabet, // should be unique...we do not test this here
            IEnumerable<Transition<char, TState>> transitions,
            TState startState,
            IEnumerable<TState> acceptStates)
        {
            _base = null;
            _next = null;
            _check = null;
            _default = null;
        }

        public int NextState(int s, char c)
        {
            int index = _base[s] + c;
            return _check[index] == s
                ? _next[index]                  // we found a compressed record
                : NextState(_default[s], c);    // we found an uncompressed record mimicked by the default state
        }
    }
}
