using System;
using System.Collections.Generic;
using System.Linq;
using AutomataLib;

namespace ContextFreeGrammar
{
    /// <summary>
    /// Simple textbook grammar, where tokens are single character letters
    /// </summary>
    public class GrammarBuilder: GrammarBuilder<Nonterminal, Terminal>
    {
    }

    public class GrammarBuilder<TNonterminalSymbol, TTerminalSymbol>
        where TNonterminalSymbol : Symbol, IEquatable<TNonterminalSymbol>
        where TTerminalSymbol : Symbol, IEquatable<TTerminalSymbol>
    {
        private IEnumerable<TNonterminalSymbol> _nonterminals;
        private IEnumerable<TTerminalSymbol> _terminals;
        private TNonterminalSymbol _startSymbol;

        public GrammarBuilder<TNonterminalSymbol, TTerminalSymbol> SetStartSymbol(TNonterminalSymbol start)
        {
            _startSymbol = start;
            return this;
        }

        public GrammarBuilder<TNonterminalSymbol, TTerminalSymbol> SetNonterminalSymbols(IEnumerable<TNonterminalSymbol> nonterminals)
        {
            _nonterminals = nonterminals ?? Enumerable.Empty<TNonterminalSymbol>();
            return this;
        }

        public GrammarBuilder<TNonterminalSymbol, TTerminalSymbol> SetTerminalSymbols(IEnumerable<TTerminalSymbol> terminals)
        {
            _terminals = terminals ?? Enumerable.Empty<TTerminalSymbol>();
            return this;
        }

        public Grammar<TNonterminalSymbol, TTerminalSymbol> AndProductions(params Production<TNonterminalSymbol>[] productions)
        {
            return new Grammar<TNonterminalSymbol, TTerminalSymbol>(_nonterminals, _terminals, _startSymbol, productions);
        }

    }
}