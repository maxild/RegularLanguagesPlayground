using System;
using System.Collections.Generic;
using AutomataLib;

namespace ContextFreeGrammar.Lexers
{
    public interface ILexer<out TToken>
    {
        TToken GetNextToken();
    }

    public interface IBufferedLexer<out TToken> : ILexer<TToken>, IReadOnlyList<TToken>
    {
        IReadOnlyList<TToken> GetRemainingTokens(int offset);
    }
}
