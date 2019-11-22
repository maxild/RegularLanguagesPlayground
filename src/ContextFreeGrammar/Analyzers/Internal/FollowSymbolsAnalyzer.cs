using System;
using AutomataLib;

namespace ContextFreeGrammar.Analyzers.Internal
{
    public class FollowSymbolsAnalyzer<TNonterminalSymbol, TTerminalSymbol> : IFollowSymbolsAnalyzer<TNonterminalSymbol, TTerminalSymbol>
        where TTerminalSymbol : IEquatable<TTerminalSymbol>
    {
        private readonly IErasableSymbolsAnalyzer _nullableSymbolsAnalyzer;
        private readonly IFirstSetsAnalyzer<TTerminalSymbol> _starterTokensAnalyzer;
        private readonly IFollowSetsAnalyzer<TNonterminalSymbol, TTerminalSymbol> _followerTokensAnalyzer;

        public FollowSymbolsAnalyzer(
            IErasableSymbolsAnalyzer nullableSymbolsAnalyzer,
            IFirstSetsAnalyzer<TTerminalSymbol> starterTokensAnalyzer,
            IFollowSetsAnalyzer<TNonterminalSymbol, TTerminalSymbol> followerTokensAnalyzer)
        {
            _nullableSymbolsAnalyzer = nullableSymbolsAnalyzer;
            _starterTokensAnalyzer = starterTokensAnalyzer;
            _followerTokensAnalyzer = followerTokensAnalyzer;
        }

        /// <inheritdoc />
        public bool Erasable(Symbol symbol) => _nullableSymbolsAnalyzer.Erasable(symbol);

        /// <inheritdoc />
        public IReadOnlySet<TTerminalSymbol> First(Symbol symbol) => _starterTokensAnalyzer.First(symbol);

        /// <inheritdoc />
        public IReadOnlySet<TTerminalSymbol> Follow(TNonterminalSymbol variable) => _followerTokensAnalyzer.Follow(variable);
    }
}
