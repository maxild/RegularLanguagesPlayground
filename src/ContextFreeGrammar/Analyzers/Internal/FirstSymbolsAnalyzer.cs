using System;
using AutomataLib;

namespace ContextFreeGrammar.Analyzers.Internal
{
    public class FirstSymbolsAnalyzer<TTokenKind> : IFirstSymbolsAnalyzer<TTokenKind>
        where TTokenKind : struct, Enum
    {
        private readonly IErasableSymbolsAnalyzer _nullableSymbolsAnalyzer;
        private readonly IFirstSetsAnalyzer<TTokenKind> _starterTokensAnalyzer;

        public FirstSymbolsAnalyzer(IErasableSymbolsAnalyzer nullableSymbolsAnalyzer, IFirstSetsAnalyzer<TTokenKind> starterTokensAnalyzer)
        {
            _nullableSymbolsAnalyzer = nullableSymbolsAnalyzer;
            _starterTokensAnalyzer = starterTokensAnalyzer;
        }

        /// <inheritdoc />
        public bool Erasable(Symbol symbol) => _nullableSymbolsAnalyzer.Erasable(symbol);

        /// <inheritdoc />
        public IReadOnlySet<Terminal<TTokenKind>> First(Symbol symbol) => _starterTokensAnalyzer.First(symbol);
    }
}
