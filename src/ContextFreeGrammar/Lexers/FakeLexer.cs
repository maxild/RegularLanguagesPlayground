using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AutomataLib;

namespace ContextFreeGrammar.Lexers
{
    public class FakeLexer<TTokenKind> : IBufferedLexer<Token<TTokenKind>>
        where TTokenKind : Enum
    {
        private readonly Token<TTokenKind>[] _tokens;
        private int _index;

        public FakeLexer(IEnumerable<(TTokenKind, string)> tokenStream)
        {
            _tokens = CreateTokenArray(tokenStream);
        }

        public FakeLexer(params (TTokenKind, string)[] tokenStream)
        {
            _tokens = CreateTokenArray(tokenStream);
        }

        static Token<TTokenKind>[] CreateTokenArray(IEnumerable<(TTokenKind, string)> tokenStream)
        {
            return (tokenStream ?? Enumerable.Empty<(TTokenKind, string)>())
                .Select(pair => new Token<TTokenKind>(pair.Item1, pair.Item2))
                .Concat(Token<TTokenKind>.EOF.AsSingletonEnumerable())
                .ToArray();
        }

        public Token<TTokenKind> GetNextToken()
        {
            return _index < _tokens.Length
                ? _tokens[_index++]
                : _tokens[^1]; // automatic append of (EOF, $) pair, and this pair is returned infinitely at end-of-input
        }

        public IReadOnlyList<Token<TTokenKind>> GetRemainingTokens(int offset)
        {
            return new ArraySegment<Token<TTokenKind>>(_tokens, offset, _tokens.Length - offset);
        }

        public int Count => _tokens.Length;

        public Token<TTokenKind> this[int index] => _tokens[index];

        public IEnumerator<Token<TTokenKind>> GetEnumerator()
        {
            return (IEnumerator<Token<TTokenKind>>)_tokens.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
