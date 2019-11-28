using System;
using AutomataLib;

namespace ContextFreeGrammar.Analyzers
{
    // LalrLookaheadSetsAnalyzer (3 metoder: digraph, dragon book, in efficient merge)
    public class Lr0AutomatonAnalyzer<TNonterminalSymbol, TTerminalSymbol>
        where TNonterminalSymbol : Symbol, IEquatable<TNonterminalSymbol>
        where TTerminalSymbol : Symbol, IEquatable<TTerminalSymbol>
    {
        public Lr0AutomatonAnalyzer(Grammar<TNonterminalSymbol, TTerminalSymbol> grammar)
        {

        }

        // StateNonterminalPairs
        // GetGotoTransitionPairs
        // SourceTransitionPair =

        // TODO
        //public IReadOnlyOrderedSet<> GotoTransitionPairs
        //{
        //    get
        //    {

        //    }
        //}


    }
}
