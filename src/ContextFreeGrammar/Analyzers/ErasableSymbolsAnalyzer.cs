using System;
using System.Collections.Generic;
using System.Linq;
using AutomataLib;

namespace ContextFreeGrammar.Analyzers
{
    internal class ErasableSymbolsAnalyzer<TTokenKind> : IErasableSymbolsAnalyzer
        where TTokenKind : Enum
    {
        private readonly Dictionary<Nonterminal, bool> _nullableMap;

        internal ErasableSymbolsAnalyzer(Grammar<TTokenKind> grammar)
        {
            if (grammar == null)
                throw new ArgumentNullException(nameof(grammar));

            _nullableMap = ComputeErasableSymbols(grammar);
        }

        /// <inheritdoc />
        public bool Erasable(Symbol symbol)
        {
            return symbol.IsEpsilon || symbol.IsEof || symbol is Nonterminal t && _nullableMap[t];
            //return symbol is TNonterminalSymbol variable
            //    ? _nullableMap[variable]
            //    : symbol.IsEpsilon || symbol.IsEof; // terminal and eof are both erasable (eof by convention)
        }

        private static Dictionary<Nonterminal, bool> ComputeErasableSymbols(Grammar<TTokenKind> grammar)
        {
            // only define nullable predicate on non-terminals
            Dictionary<Nonterminal, bool> nullableMap = grammar.Variables.ToDictionary(symbol => symbol, _ => false);

            bool changed = true;
            while (changed)
            {
                changed = false;
                // For each production X → Y1 Y2...Yn
                foreach (var production in grammar.Productions)
                {
                    if (false == nullableMap[production.Head])
                    {
                        // if all symbols Y1 Y2...Yn are nullable (e.g. if X is an ε-production)
                        if (production.Tail.Count == 0 ||
                            production.Tail.All(symbol => symbol.IsEpsilon || symbol is Nonterminal t && nullableMap[t]))
                        {
                            nullableMap[production.Head] = true;
                            changed = true;
                        }
                    }
                }
            }

            return nullableMap;
        }
    }
}
