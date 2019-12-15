using System;
using AutomataLib;

namespace ContextFreeGrammar.Analyzers.Internal
{
    public class FollowSymbolsAnalyzer<TTokenKind> : IFollowSymbolsAnalyzer<TTokenKind>
        where TTokenKind : struct, Enum
    {
        private readonly IErasableSymbolsAnalyzer _nullableSymbolsAnalyzer;
        private readonly IFirstSetsAnalyzer<TTokenKind> _starterTokensAnalyzer;
        private readonly IFollowSetsAnalyzer<TTokenKind> _followerTokensAnalyzer;

        public FollowSymbolsAnalyzer(
            IErasableSymbolsAnalyzer nullableSymbolsAnalyzer,
            IFirstSetsAnalyzer<TTokenKind> starterTokensAnalyzer,
            IFollowSetsAnalyzer<TTokenKind> followerTokensAnalyzer)
        {
            _nullableSymbolsAnalyzer = nullableSymbolsAnalyzer;
            _starterTokensAnalyzer = starterTokensAnalyzer;
            _followerTokensAnalyzer = followerTokensAnalyzer;
        }

        /// <inheritdoc />
        public bool Erasable(Symbol symbol) => _nullableSymbolsAnalyzer.Erasable(symbol);

        /// <inheritdoc />
        public IReadOnlySet<Terminal<TTokenKind>> First(Symbol symbol) => _starterTokensAnalyzer.First(symbol);

        /// <inheritdoc />
        public IReadOnlySet<Terminal<TTokenKind>> Follow(Nonterminal variable) => _followerTokensAnalyzer.Follow(variable);
    }
}
