using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AutomataLib;

namespace ContextFreeGrammar.Lexers
{
    public class FakeLexer<TEnum> : IBufferedLexer<Token<TEnum>>
        where TEnum : Enum
    {
        private readonly Token<TEnum>[] _tokens;
        private int _index;

        public FakeLexer(IEnumerable<(TEnum, string)> tokenStream)
        {
            _tokens = CreateTokenArray(tokenStream);
        }

        public FakeLexer(params (TEnum, string)[] tokenStream)
        {
            _tokens = CreateTokenArray(tokenStream);
        }

        static Token<TEnum>[] CreateTokenArray(IEnumerable<(TEnum, string)> tokenStream)
        {
            return (tokenStream ?? Enumerable.Empty<(TEnum, string)>())
                .Select(pair => new Token<TEnum>(pair.Item1, pair.Item2))
                .Concat(Token<TEnum>.EOF.AsSingletonEnumerable())
                .ToArray();
        }

        public Token<TEnum> GetNextToken()
        {
            return _index < _tokens.Length
                ? _tokens[_index++]
                : _tokens[^1]; // automatic append of (EOF, $) pair, and this pair is returned infinitely at end-of-input
        }

        public IReadOnlyList<Token<TEnum>> GetRemainingTokens(int offset)
        {
            return new ArraySegment<Token<TEnum>>(_tokens, offset, _tokens.Length - offset);
        }

        public int Count => _tokens.Length;

        public Token<TEnum> this[int index] => _tokens[index];

        public IEnumerator<Token<TEnum>> GetEnumerator()
        {
            return (IEnumerator<Token<TEnum>>)_tokens.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
