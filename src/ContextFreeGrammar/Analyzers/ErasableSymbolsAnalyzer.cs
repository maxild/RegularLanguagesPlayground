using System;
using System.Collections.Generic;
using System.Linq;
using AutomataLib;

namespace ContextFreeGrammar.Analyzers
{
    public class ErasableSymbolsAnalyzer<TNonterminalSymbol, TTerminalSymbol> : IErasableSymbolsAnalyzer
        where TTerminalSymbol : Symbol, IEquatable<TTerminalSymbol>
        where TNonterminalSymbol : Symbol, IEquatable<TNonterminalSymbol>
    {
        private readonly Dictionary<TNonterminalSymbol, bool> _nullableMap;

        public ErasableSymbolsAnalyzer(Grammar<TNonterminalSymbol, TTerminalSymbol> grammar)
        {
            if (grammar == null)
                throw new ArgumentNullException(nameof(grammar));

            _nullableMap = ComputeErasableSymbols(grammar);
        }

        public bool Erasable(Symbol symbol)
        {
            return symbol.IsEpsilon || symbol is TNonterminalSymbol t && _nullableMap[t];
            //return symbol is TNonterminalSymbol variable
            //    ? _nullableMap[variable]
            //    : symbol.IsEpsilon; // terminal and eof are not erasable
        }

        private static Dictionary<TNonterminalSymbol, bool> ComputeErasableSymbols(Grammar<TNonterminalSymbol, TTerminalSymbol> grammar)
        {
            // only define nullable predicate on non-terminals
            Dictionary<TNonterminalSymbol, bool> nullableMap = grammar.Variables.ToDictionary(symbol => symbol, _ => false);

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
                            production.Tail.All(symbol => symbol.IsEpsilon
                                                          || symbol is TNonterminalSymbol t && nullableMap[t]))
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
