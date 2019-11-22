using System;
using AutomataLib;

namespace ContextFreeGrammar.Analyzers.Internal
{
    public class FirstSymbolsAnalyzer<TTerminalSymbol> : IFirstSymbolsAnalyzer<TTerminalSymbol>
        where TTerminalSymbol : IEquatable<TTerminalSymbol>
    {
        private readonly IErasableSymbolsAnalyzer _nullableSymbolsAnalyzer;
        private readonly IFirstSetsAnalyzer<TTerminalSymbol> _starterTokensAnalyzer;

        public FirstSymbolsAnalyzer(IErasableSymbolsAnalyzer nullableSymbolsAnalyzer, IFirstSetsAnalyzer<TTerminalSymbol> starterTokensAnalyzer)
        {
            _nullableSymbolsAnalyzer = nullableSymbolsAnalyzer;
            _starterTokensAnalyzer = starterTokensAnalyzer;
        }

        /// <inheritdoc />
        public bool Erasable(Symbol symbol) => _nullableSymbolsAnalyzer.Erasable(symbol);

        /// <inheritdoc />
        public IReadOnlySet<TTerminalSymbol> First(Symbol symbol) => _starterTokensAnalyzer.First(symbol);
    }
}
