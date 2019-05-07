using System;
using System.Collections.Generic;

namespace AutomataLib
{
    public interface IShiftReduceParser<TNonterminalSymbol, TTerminalSymbol>
        where TNonterminalSymbol : Symbol, IEquatable<TNonterminalSymbol>
        where TTerminalSymbol : Symbol, IEquatable<TTerminalSymbol>
    {
        IEnumerable<TTerminalSymbol> TerminalSymbols { get; }
        IEnumerable<TNonterminalSymbol> NonTerminalSymbols { get; }
        IEnumerable<TNonterminalSymbol> TrimmedNonTerminalSymbols { get; }
        IEnumerable<int> GetStates();
        LrAction Action(int state, TTerminalSymbol token);
        int Goto(int state, TNonterminalSymbol variable);
    }
}
