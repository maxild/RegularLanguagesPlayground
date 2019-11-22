using System;
using AutomataLib;
using ContextFreeGrammar.Analyzers.Internal;

namespace ContextFreeGrammar.Analyzers
{
    public static class Analyzers
    {
        public static IFollowSymbolsAnalyzer<TNonterminalSymbol, TTerminalSymbol> CreateDragonBookAnalyzer<TNonterminalSymbol, TTerminalSymbol>(
            Grammar<TNonterminalSymbol, TTerminalSymbol> grammar)
            where TTerminalSymbol : Symbol, IEquatable<TTerminalSymbol>
            where TNonterminalSymbol : Symbol, IEquatable<TNonterminalSymbol>
        {
            return new DragonBookAnalyzer<TNonterminalSymbol, TTerminalSymbol>(grammar);
        }

        public static IFollowSymbolsAnalyzer<TNonterminalSymbol, TTerminalSymbol> CreateDigraphAlgorithmAnalyzer<TNonterminalSymbol, TTerminalSymbol>(Grammar<TNonterminalSymbol, TTerminalSymbol> grammar)
            where TTerminalSymbol : Symbol, IEquatable<TTerminalSymbol>
            where TNonterminalSymbol : Symbol, IEquatable<TNonterminalSymbol>
        {
            var nullableSymbolsAnalyzer
                = new ErasableSymbolsAnalyzer<TNonterminalSymbol, TTerminalSymbol>(grammar);

            var starterTokensAnalyzer =
                new FirstSetsDigraphAnalyzer<TNonterminalSymbol, TTerminalSymbol>(grammar, nullableSymbolsAnalyzer);

            var followerTokensAnalyzer
                = new FollowSetsDigraphAnalyzer<TNonterminalSymbol, TTerminalSymbol>(grammar, nullableSymbolsAnalyzer, starterTokensAnalyzer);

            return new FollowSymbolsAnalyzer<TNonterminalSymbol, TTerminalSymbol>(nullableSymbolsAnalyzer,
                starterTokensAnalyzer, followerTokensAnalyzer);
        }

        public static IErasableSymbolsAnalyzer CreateErasableSymbolsAnalyzer<TNonterminalSymbol,
            TTerminalSymbol>(Grammar<TNonterminalSymbol, TTerminalSymbol> grammar)
            where TTerminalSymbol : Symbol, IEquatable<TTerminalSymbol>
            where TNonterminalSymbol : Symbol, IEquatable<TNonterminalSymbol>
        {
            return new ErasableSymbolsAnalyzer<TNonterminalSymbol, TTerminalSymbol>(grammar);
        }

        public static IFirstSymbolsAnalyzer<TTerminalSymbol> CreateFirstSymbolsAnalyzer<TNonterminalSymbol, TTerminalSymbol>(Grammar<TNonterminalSymbol, TTerminalSymbol> grammar)
            where TTerminalSymbol : Symbol, IEquatable<TTerminalSymbol>
            where TNonterminalSymbol : Symbol, IEquatable<TNonterminalSymbol>
        {
            var nullableSymbolsAnalyzer
                = new ErasableSymbolsAnalyzer<TNonterminalSymbol, TTerminalSymbol>(grammar);

            var starterTokensAnalyzer =
                new FirstSetsDigraphAnalyzer<TNonterminalSymbol, TTerminalSymbol>(grammar, nullableSymbolsAnalyzer);

            return new FirstSymbolsAnalyzer<TTerminalSymbol>(nullableSymbolsAnalyzer,
                starterTokensAnalyzer);
        }
    }
}
