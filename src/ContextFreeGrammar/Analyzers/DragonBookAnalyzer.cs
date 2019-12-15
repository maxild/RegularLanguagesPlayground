using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using AutomataLib;

namespace ContextFreeGrammar.Analyzers
{
    /// <summary>
    /// FixedPointIteration Algorithm found in most textbooks on compilers
    /// </summary>
    public class DragonBookAnalyzer<TTokenKind> : IFollowSymbolsAnalyzer<TTokenKind>
        where TTokenKind : struct, Enum
    {
        private readonly Dictionary<Symbol, bool> _nullableMap;
        private readonly Dictionary<Symbol, Set<Terminal<TTokenKind>>> _firstMap;
        private readonly Dictionary<Nonterminal, Set<Terminal<TTokenKind>>> _followMap;

        public DragonBookAnalyzer(Grammar<TTokenKind> grammar)
        {
            (_nullableMap, _firstMap, _followMap) = ComputeFollow(grammar);
        }

        public bool Erasable(Symbol symbol) => _nullableMap[symbol];

        public IReadOnlySet<Terminal<TTokenKind>> First(Symbol symbol) => _firstMap[symbol];

        public IReadOnlySet<Terminal<TTokenKind>> Follow(Nonterminal variable)
            => _followMap[variable];

        // This method is kept around, because we might need to calculate nullable predicate, if calculating
        // FIRST and FOLLOW sets using a Graph representing all the recursive set constraints as a relation and
        // using Graph traversal as an efficient iteration technique to solve for the unique least fixed-point solution.
        private static Dictionary<Symbol, bool> ComputeNullable(Grammar<TTokenKind> grammar)
        {
            var nullableMap = grammar.AllSymbols.ToDictionary(symbol => symbol, symbol => symbol.IsEpsilon || symbol.IsEof);

            if (!nullableMap.ContainsKey(Symbol.Eof<TTokenKind>()))
                nullableMap.Add(Symbol.Eof<TTokenKind>(), true); // by convention

            bool changed = true;
            while (changed)
            {
                changed = false;
                // For each production X → Y1 Y2...Yn
                foreach (var production in grammar.Productions)
                {
                    if (!nullableMap[production.Head])
                    {
                        // if all symbols Y1 Y2...Yn are nullable (e.g. if X is an ε-production)
                        if (production.Tail.Count == 0 ||
                            production.Tail.All(symbol => nullableMap[symbol]))
                        {
                            nullableMap[production.Head] = true;
                            changed = true;
                        }
                    }
                }
            }

            return nullableMap;
        }

        // From the dragon book...we compute nullable predicate in separate least fixed-point iteration, and
        // make First only have range of terminal symbols (that is First do not contain epsilon in my implementation)
        //=======================================================================================================
        // To compute First(X) for all grammar symbols, apply the Following rules until no more terminals or ε can
        // be added to any First set.
        //
        //    1. If X is terminal, then First(X) is X.
        //    2. If X→ ε is a production, then add ε to First(X).
        //    3. If X is non-terminal and X → Y1 Y2...YK is a production, then place a in First(X) if for some i, a
        //       is in First(Yi), and ε is in all of First(Y1)...First(Yi-1); that is, Y1...Yi-1 *=> ε. If ε is
        //       in First(Yj) for all j=1, 2, ⋯, k, then add ε to First(X).
        //
        // Now define First(α) for any string α = X1X2…Xn as follows. First(α) contains First(X1)-{ε}.For each
        // i=2,…,n, if First(Xk) contains ε for all k=1,…,i-1, then First(α) contains First(Xi)-{ε}.Finally,
        // if for all i=1,…,n, First(Xi) contains ε, then First(α) contains ε.
        //=======================================================================================================
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static (Dictionary<Symbol, bool>, Dictionary<Symbol, Set<Terminal<TTokenKind>>>) ComputeFirst(
            Grammar<TTokenKind> grammar)
        {
            var nullableMap = ComputeNullable(grammar);
            var firstMap = grammar.AllSymbols.ToDictionary(symbol => symbol, _ => new Set<Terminal<TTokenKind>>());

            // Base case: First(a) = {a} for all terminal symbols a ∈ T.
            foreach (var terminal in grammar.Terminals)
                firstMap[terminal].Add(terminal);

            // Add EOF to avoid unnecessary exceptions
            if (!firstMap.ContainsKey(Symbol.Eof<TTokenKind>()))
                firstMap.Add(Symbol.Eof<TTokenKind>(), new Set<Terminal<TTokenKind>> { Symbol.Eof<TTokenKind>() });

            // Simple brute-force Fixed-Point Iteration inspired by Dragon Book
            bool changed = true;
            while (changed)
            {
                changed = false;
                // For each production X → Y1 Y2...Yn
                foreach (var production in grammar.Productions)
                {
                    // FirstDelta(X) = FIRST(Y1) U ... U FIRST(Yk), where k, 1 <= k <= n, is
                    // the largest integer such that Nullable(Y1) = ... = Nullable(Yk) = true
                    // is added to First(X)
                    for (int i = 0; i < production.Length; i += 1)
                    {
                        var Yi = production.Tail[i];
                        if (firstMap[production.Head].AddRange(firstMap[Yi]))
                            changed = true;
                        if (!nullableMap[Yi])
                            break;
                    }
                }
            }

            return (nullableMap, firstMap);
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public (Dictionary<Symbol, bool>,
            Dictionary<Symbol, Set<Terminal<TTokenKind>>>,
            Dictionary<Nonterminal, Set<Terminal<TTokenKind>>>) ComputeFollow(Grammar<TTokenKind> grammar)
        {
            var (nullableMap, firstMap) = ComputeFirst(grammar);

            // We define Follow only for nonterminal symbols
            var followMap = grammar.Nonterminals.ToDictionary(symbol => symbol, _ => new Set<Terminal<TTokenKind>>());

            // NOTE: This is a requirement of the parsing table of shift-reduce parser
            // We only need to place Eof ('$' in the dragon book) in FOLLOW(S) if the grammar haven't
            // already been extended with a new nonterminal start symbol S' and a production S' → S$ in P.
            if (!grammar.IsAugmentedWithEofMarker)
                followMap[grammar.AugmentedStartItem.GetDotSymbol<Nonterminal>()].Add(Symbol.Eof<TTokenKind>());

            // Simple brute-force Fixed-Point Iteration inspired by Dragon Book
            bool changed = true;
            while (changed)
            {
                changed = false;
                // For each production X → Y1 Y2...Yn
                foreach (var production in grammar.Productions)
                {
                    for (int i = 0; i < production.Length; i += 1)
                    {
                        // for each Yi that is a nonterminal symbol
                        var Yi = production.TailAs<Nonterminal>(i);
                        if (Yi == null) continue;
                        // Let m = First(Y(i+1)...Yn)
                        var m = First(production.Tail.Skip(i + 1));
                        // add m to Follow(Yi)
                        changed = followMap[Yi].AddRange(m) || changed;
                        // add Follow(X) to Follow(Yi)
                        if (!Yi.Equals(production.Head) && Nullable(production.Tail.Skip(i + 1)))
                            changed = followMap[Yi].AddRange(followMap[production.Head]) || changed;
                    }
                }
            }

            return (nullableMap, firstMap, followMap);

            // extend First to words of symbols
            Set<Terminal<TTokenKind>> First(IEnumerable<Symbol> symbols)
            {
                var m = new Set<Terminal<TTokenKind>>();
                foreach (var symbol in symbols)
                {
                    m.AddRange(firstMap[symbol]);
                    if (!nullableMap[symbol]) break;
                }

                return m;
            }

            // extend Nullable to words of symbols
            bool Nullable(IEnumerable<Symbol> symbols)
            {
                return symbols.All(symbol => nullableMap[symbol]);
            }
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private (Dictionary<Symbol, bool>,
            Dictionary<Symbol, Set<Terminal<TTokenKind>>>,
            Dictionary<Nonterminal, Set<Terminal<TTokenKind>>>) OldComputeNullableAndFirstAndFollow(Grammar<TTokenKind> grammar)
        {
            // we keep a separate nullable map, instead of adding epsilon to First sets
            var nullableMap = grammar.AllSymbols.ToDictionary(symbol => symbol, symbol => symbol.IsEpsilon);
            var firstMap = grammar.AllSymbols.ToDictionary(symbol => symbol, _ => new Set<Terminal<TTokenKind>>());
            var followMap = grammar.Nonterminals.ToDictionary(symbol => symbol, _ => new Set<Terminal<TTokenKind>>());

            // Base case: First(a) = {a} for all terminal symbols a ∈ T.
            foreach (var symbol in grammar.Terminals)
                firstMap[symbol].Add(symbol);

            // We only need to place Eof ('$' in the dragon book) in FOLLOW(S) if the grammar haven't
            // already been extended with a new nonterminal start symbol S' and a production S' → S$ in P.
            if (!grammar.IsAugmentedWithEofMarker)
                followMap[grammar.StartSymbol].Add(Symbol.Eof<TTokenKind>());

            bool changed = true;
            while (changed)
            {
                changed = false;
                // For each production X → Y1 Y2...Yn
                foreach (var production in grammar.Productions)
                {
                    if (!nullableMap[production.Head])
                    {
                        // if all symbols Y1 Y2...Yn are nullable (e.g. if X is an ε-production)
                        if (production.Tail.Count == 0 ||
                            production.Tail.All(symbol => nullableMap[symbol]))
                        {
                            nullableMap[production.Head] = true;
                            changed = true;
                        }
                    }

                    // For each RHS symbol in production X → Y1 Y2...Yn
                    for (var i = 0; i < production.Length; i++)
                    {
                        // X → Y1 Y2...Yn
                        Symbol yi = production.Tail[i]; // FIRST is extended to all symbols

                        // If all symbols Y1 Y2...Y(i-1) preceding Yi are nullable,
                        if (i == 0 || production.Tail.Take(i).All(symbol => nullableMap[symbol]))
                        {
                            // then everything in First(Yi) is in First(X)
                            if (firstMap[production.Head].AddRange(firstMap[yi]))
                                changed = true;
                        }

                        // for each Yi that is a nonterminal symbol
                        var Yi = yi as Nonterminal; // FOLLOW is only defined w.r.t. nonterminal symbols.
                        if (Yi == null) continue;

                        // If all symbols Y(i+1)...Yn succeeding Yi are nullable,
                        if (i == production.Tail.Count - 1 ||
                            production.Tail.Skip(i + 1).All(symbol => nullableMap[symbol]))
                        {
                            // then everything in Follow(X) is in Follow(Yi)
                            if (followMap[Yi].AddRange(followMap[production.Head]))
                                changed = true;
                        }

                        // Here we try to add everything in First(Y(i+1)...Yn) to Follow(Yi)
                        // For each symbol Y(j) in Y(i+1)...Yn, e.g. for each Y(j) succeeding Y(i)
                        for (var j = i + 1; j < production.Tail.Count; j += 1)
                        {
                            var Yj = production.Tail[j];
                            // If the sequence Y(i+1)...Y(j-1) is nullable (or empty,
                            // as is the case for the first iteration),
                            if (j > i + 1 && !production.Tail.Skip(i + 1).Take(j - i - 1)
                                    .All(symbol => nullableMap[symbol]))
                                break;

                            // then everything in First(Y(j)) is in Follow(Y(i))
                            if (followMap[Yi].AddRange(firstMap[Yj]))
                                changed = true;
                        }
                    }
                }
            }

            return (nullableMap, firstMap, followMap);
        }
    }
}
