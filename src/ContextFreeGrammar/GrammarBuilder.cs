using System;
using System.Collections.Generic;
using System.Linq;
using ContextFreeGrammar.Analyzers;

namespace ContextFreeGrammar
{
    public class GrammarBuilder<TTokenKind> where TTokenKind : struct, Enum
    {
        private IEnumerable<Nonterminal> _nonterminals;
        private readonly IReadOnlyList<Terminal<TTokenKind>> _terminals;
        private Nonterminal _startSymbol;
        private Func<Grammar<TTokenKind>, IFollowSymbolsAnalyzer<TTokenKind>> _analyzerFactory;

        public GrammarBuilder()
        {
            _terminals = Terminal<TTokenKind>.All;
        }

        public GrammarBuilder<TTokenKind> SetStartSymbol(Nonterminal start)
        {
            _startSymbol = start;
            return this;
        }

        public GrammarBuilder<TTokenKind> SetNonterminalSymbols(IEnumerable<Nonterminal> nonterminals)
        {
            _nonterminals = nonterminals ?? Enumerable.Empty<Nonterminal>();
            return this;
        }

        public GrammarBuilder<TTokenKind> SetAnalyzer(
            Func<Grammar<TTokenKind>, IFollowSymbolsAnalyzer<TTokenKind>> analyzerFactory)
        {
            _analyzerFactory = analyzerFactory;
            return this;
        }

        public Grammar<TTokenKind> AndProductions(params Production[] productions)
        {
            return new Grammar<TTokenKind>(_nonterminals, _terminals, _startSymbol, productions,
                _analyzerFactory ?? (grammar => new DragonBookAnalyzer<TTokenKind>(grammar)));
        }
    }
}
