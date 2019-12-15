using System;
using ContextFreeGrammar.Analyzers.Internal;

namespace ContextFreeGrammar.Analyzers
{
    public static class Analyzers
    {
        public static IFollowSymbolsAnalyzer<TTokenKind> CreateDragonBookAnalyzer<TTokenKind>(Grammar<TTokenKind> grammar)
            where TTokenKind : struct, Enum
        {
            return new DragonBookAnalyzer<TTokenKind>(grammar);
        }

        public static IFollowSymbolsAnalyzer<TTokenKind> CreateDigraphAlgorithmAnalyzer<TTokenKind>(Grammar<TTokenKind> grammar)
            where TTokenKind : struct, Enum
        {
            var nullableSymbolsAnalyzer
                = new ErasableSymbolsAnalyzer<TTokenKind>(grammar);

            var starterTokensAnalyzer =
                new FirstSetsDigraphAnalyzer<TTokenKind>(grammar, nullableSymbolsAnalyzer);

            var followerTokensAnalyzer
                = new FollowSetsDigraphAnalyzer<TTokenKind>(grammar, nullableSymbolsAnalyzer, starterTokensAnalyzer);

            return new FollowSymbolsAnalyzer<TTokenKind>(nullableSymbolsAnalyzer,
                starterTokensAnalyzer, followerTokensAnalyzer);
        }

        public static IErasableSymbolsAnalyzer CreateErasableSymbolsAnalyzer<TTokenKind>(Grammar<TTokenKind> grammar)
            where TTokenKind : struct, Enum
        {
            return new ErasableSymbolsAnalyzer<TTokenKind>(grammar);
        }

        public static IFirstSymbolsAnalyzer<TTokenKind> CreateFirstSymbolsAnalyzer<TTokenKind>(Grammar<TTokenKind> grammar)
            where TTokenKind : struct, Enum
        {
            var nullableSymbolsAnalyzer = new ErasableSymbolsAnalyzer<TTokenKind>(grammar);

            var starterTokensAnalyzer = new FirstSetsDigraphAnalyzer<TTokenKind>(grammar, nullableSymbolsAnalyzer);

            return new FirstSymbolsAnalyzer<TTokenKind>(nullableSymbolsAnalyzer, starterTokensAnalyzer);
        }
    }
}
