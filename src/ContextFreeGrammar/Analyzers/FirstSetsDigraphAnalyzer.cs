using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using AutomataLib;

namespace ContextFreeGrammar.Analyzers
{
    internal class FirstSetsDigraphAnalyzer<TNonterminalSymbol, TTerminalSymbol> : IFirstSetsAnalyzer<TTerminalSymbol>
        where TTerminalSymbol : Symbol, IEquatable<TTerminalSymbol>
        where TNonterminalSymbol : Symbol, IEquatable<TNonterminalSymbol>
    {
        private static readonly Set<TTerminalSymbol> s_eofSingleton = new Set<TTerminalSymbol>(new []{Symbol.Eof<TTerminalSymbol>()});
        private static readonly Set<TTerminalSymbol> s_empty = new Set<TTerminalSymbol>();

        private readonly Dictionary<TNonterminalSymbol, Set<TTerminalSymbol>> _firstMap;

        internal FirstSetsDigraphAnalyzer(Grammar<TNonterminalSymbol, TTerminalSymbol> grammar, IErasableSymbolsAnalyzer analyzer)
        {
            _firstMap = ComputeFirst(grammar, analyzer);
        }

        /// <inheritdoc />
        public IReadOnlySet<TTerminalSymbol> First(Symbol symbol)
        {
            if (symbol is TNonterminalSymbol variable)
                return _firstMap[variable];
            if (symbol is TTerminalSymbol token)
                return new Set<TTerminalSymbol>(new []{token});
            if (symbol.IsEof)
                return s_eofSingleton;
            return s_empty; // epsilon
        }

        // We can collectively characterize all FIRST sets as the smallest sets
        // FIRST(A) satisfying, for each A in N:
        //    (i)  FIRST(A) contains INITFIRST(A), and
        //    (ii) for each nonterminal B, s.t. A contains_the_first_set_of B:
        //                 FIRST(A) contains FIRST(B).
        // By the way, this is equivalent to
        //
        // FIRST(A) = INITFIRST(A) ∪ { FIRST(B) : (A,B) ∈ contains_the_first_set_of+ }
        //                            -- or --
        // FIRST(A) = ∪ { INITFIRST(B) : A contains_the_first_set_of* B }.
        //                            -- or --
        // FIRST(A) = ∪ { INITFIRST(B) : (A,B) ∈ contains_the_first_set_of* }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private Dictionary<TNonterminalSymbol, Set<TTerminalSymbol>> ComputeFirst(
            Grammar<TNonterminalSymbol, TTerminalSymbol> grammar, IErasableSymbolsAnalyzer analyzer)
        {
            var (initFirstSets, graph) = DigraphAlgorithm.GetFirstGraph(grammar, analyzer);

            var firstSets = DigraphAlgorithm.Traverse(grammar, graph, initFirstSets);

            var firstMap = grammar.Variables.ToDictionary(v => v, v => firstSets[grammar.Variables.IndexOf(v)]);

            return firstMap;
        }

    }
}
