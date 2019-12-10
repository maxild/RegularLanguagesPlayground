using System;
using System.Collections.Generic;
using System.Linq;
using AutomataLib;
using ContextFreeGrammar.Analyzers;

namespace ContextFreeGrammar
{
    public class GrammarBuilder<TTokenKind>
        where TTokenKind : Enum
    {
        private IEnumerable<Nonterminal> _nonterminals;
        private readonly IEnumerable<Terminal<TTokenKind>> _terminals;
        private Nonterminal _startSymbol;
        private Func<Grammar<TTokenKind>, IFollowSymbolsAnalyzer<TTokenKind>> _analyzerFactory;

        public GrammarBuilder()
        {
            // TODO: We could do better here
            //     indexed array, where each token kind value is the index
            //     Name     (Enum name)
            //     Keyword  (+ for PLUS)
            //     Index    (Enum value as int)
            //     Kind     (Enum)
            _terminals = Enum.GetValues(typeof(TTokenKind)).Cast<TTokenKind>()
                //.Where(kind => !kind.Equals(default)) // TODO: Find better way to filter out EPS (epsilon tokens)
                .Where(kind => false == "EPS".Equals(Enum.GetName(typeof(TTokenKind), kind), StringComparison.Ordinal))
                .Select(Symbol.T)
                .ToArray();
        }

        public GrammarBuilder<TTokenKind> SetStartSymbol(Nonterminal start)
        {
            _startSymbol = start;
            // EPS token kind
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
