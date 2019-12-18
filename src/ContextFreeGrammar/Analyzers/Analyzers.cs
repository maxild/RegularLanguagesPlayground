using System;
using ContextFreeGrammar.Analyzers.Internal;

namespace ContextFreeGrammar.Analyzers
{
    public static class Analyzers
    {
        public static IFollowSymbolsAnalyzer<TTokenKind> CreateDragonBookAnalyzer<TTokenKind, TNonterminal>(
            Grammar<TTokenKind, TNonterminal> grammar)
            where TTokenKind : struct, Enum
            where TNonterminal : struct, Enum
        {
            return new DragonBookAnalyzer<TTokenKind, TNonterminal>(grammar);
        }

        public static IFollowSymbolsAnalyzer<TTokenKind> CreateDigraphAlgorithmAnalyzer<TTokenKind, TNonterminal>(
            Grammar<TTokenKind, TNonterminal> grammar)
            where TTokenKind : struct, Enum
            where TNonterminal : struct, Enum
        {
            var nullableSymbolsAnalyzer
                = new ErasableSymbolsAnalyzer<TTokenKind, TNonterminal>(grammar);

            var starterTokensAnalyzer =
                new FirstSetsDigraphAnalyzer<TTokenKind, TNonterminal>(grammar, nullableSymbolsAnalyzer);

            var followerTokensAnalyzer
                = new FollowSetsDigraphAnalyzer<TTokenKind, TNonterminal>(grammar, nullableSymbolsAnalyzer, starterTokensAnalyzer);

            return new FollowSymbolsAnalyzer<TTokenKind>(nullableSymbolsAnalyzer,
                starterTokensAnalyzer, followerTokensAnalyzer);
        }

        public static IErasableSymbolsAnalyzer CreateErasableSymbolsAnalyzer<TTokenKind, TNonterminal>(Grammar<TTokenKind, TNonterminal> grammar)
            where TTokenKind : struct, Enum
            where TNonterminal : struct, Enum
        {
            return new ErasableSymbolsAnalyzer<TTokenKind, TNonterminal>(grammar);
        }

        public static IFirstSymbolsAnalyzer<TTokenKind> CreateFirstSymbolsAnalyzer<TTokenKind, TNonterminal>(
            Grammar<TTokenKind, TNonterminal> grammar)
            where TTokenKind : struct, Enum
            where TNonterminal : struct, Enum
        {
            var nullableSymbolsAnalyzer = new ErasableSymbolsAnalyzer<TTokenKind, TNonterminal>(grammar);

            var starterTokensAnalyzer = new FirstSetsDigraphAnalyzer<TTokenKind, TNonterminal>(grammar, nullableSymbolsAnalyzer);

            return new FirstSymbolsAnalyzer<TTokenKind>(nullableSymbolsAnalyzer, starterTokensAnalyzer);
        }
    }
}
