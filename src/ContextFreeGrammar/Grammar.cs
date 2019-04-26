using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using AutomataLib;

namespace ContextFreeGrammar
{
    // First and Follow functions associated with a grammar G is important when conducting LL and
    // LR (SLR(1), LR(1), LALR(1)) parser, because the setting up of parsing table is aided by them

    //  If α is any string of grammar symbols, let First(α) be the set of terminals that begin the
    // strings derived from α, if α *=> ε, then ε is also in First(α).

    //  Define Follow(A), for non-terminal A, to be the set of terminals a that can appear immediately
    // to the right of A in some sentential form. That is, the set of terminals a such that there
    // exists a derivation of the form S *=> αAaβ for some α and β. Note that there may, at some time
    // during the derivation, have been symbols between A and a, but if so, they derived ε and disappeared.

    /// <summary>
    /// Context-free grammar (CFG)
    /// </summary>
    public class Grammar : IEnumerable<Production>
    {
        private readonly List<Production> _productions;
        private readonly Dictionary<NonTerminal, List<Production>> _productionMap;

        private int _version;

        private int _fixedPointVersion;
        //private bool[] _nullableProduction; // by production
        //private bool[] _nullableVariable;   // by nonterminal

        //private int _firstVersion; // TODO: FIRST requires NULLABLE => single version/initialization

        public Grammar(IEnumerable<NonTerminal> variables, IEnumerable<Terminal> terminals, NonTerminal startSymbol)
        {
            _productions = new List<Production>();                          // insertion ordered list
            Variables = new InsertionOrderedSet<NonTerminal>(variables);    // insertion ordered set
            Terminals = new Set<Terminal>(terminals);                       // unordered set
            StartSymbol = startSymbol;
            _productionMap = Variables.ToDictionary(symbol => symbol, _ => new List<Production>());
        }

        // TODO: Dollar should be configurable or else Symbol.Eof should be defined as special terminal
        //public Terminal Eof { get; }

        /// <summary>
        /// First production is a unit production (S -> E), where the head variable (S) has only this single production,
        /// and the head variable (S) is found no where else in any productions.
        /// </summary>
        public bool IsAugmented =>
            Productions[0].Head.Equals(StartSymbol) &&
            Productions.Skip(1).All(p => !p.Head.Equals(StartSymbol)) &&
            Productions.All(p => !p.Tail.Contains(StartSymbol));

        public bool IsAugmentedWithEofMarker => IsAugmented && Productions[0].LastSymbol.IsEof;

        // TODO: No useless symbols (required to construct DFA of LR(0) automaton, Knuths Theorem)
        public bool IsReduced => true;

        /// <summary>
        /// Nonterminal grammar symbols (aka grammar variables).
        /// </summary>
        public IReadOnlyOrderedSet<NonTerminal> Variables { get; }

        public IEnumerable<Symbol> NonTerminalSymbols => Variables;

        /// <summary>
        /// Terminal grammar symbols, not including ε (the empty string)
        /// </summary>
        public IReadOnlySet<Terminal> Terminals { get; }

        public IEnumerable<Symbol> TerminalSymbols => Terminals;

        /// <summary>
        /// All grammar symbols (terminal and nonterminal symbols), not including ε (the empty string).
        /// </summary>
        public IEnumerable<Symbol> Symbols => NonTerminalSymbols.Concat(TerminalSymbols);

        /// <summary>
        /// All grammar symbols (terminal and nonterminal symbols), including ε (the empty string).
        /// </summary>
        public IEnumerable<Symbol> AllSymbols => Symbols.Concat(Symbol.Epsilon.AsSingletonEnumerable());

        /// <summary>
        /// Productions are numbered by index 0,1,2,...
        /// </summary>
        public IReadOnlyList<Production> Productions => _productions;

        ///// <summary>
        ///// Production rules for any given variable (nonterminal symbol).
        ///// </summary>
        //public IReadOnlyDictionary<NonTerminal, IReadOnlyList<Production>> ProductionsFor =>
        //    (IReadOnlyDictionary<NonTerminal, IReadOnlyList<Production>>) _productionMap;

        /// <summary>
        /// The start symbol.
        /// </summary>
        public NonTerminal StartSymbol { get; }

        public void Add(Production production)
        {
            if (production == null)
            {
                throw new ArgumentNullException(nameof(production));
            }

            if (!_productionMap.ContainsKey(production.Head))
            {
                throw new ArgumentException($"The production {production} cannot be added, because the head of the production {production.Head} has not been defined to be variable (nonterminal symbol) of the grammar.");
            }

            if (production.IsNotEpsilon && !production.Tail.All(symbol => Symbols.Contains(symbol)))
            {
                throw new ArgumentException($"The production {production} cannot bed added, because some of the RHS symbols has not been defined to be symbols of the grammar.");
            }

            _productions.Add(production);
            _productionMap[production.Head].Add(production);
            _version += 1;
        }



        /// <summary>
        /// Does the production P(i) derive the empty string such that the nonterminal (LHS) can be erased.
        /// </summary>
        /// <param name="productionIndex">The index of a production in the Grammar.</param>
        /// <returns>True, if the production is nullable (such that the nonterminal symbol is erasable).</returns>
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public bool NULLABLE(int productionIndex)
        {
            // Nullable(X) = Nullable(Y1) Λ ... Λ Nullable(Yn), for X → Y1 Y2...Yn
            return Productions[productionIndex].Tail.All(symbol => Nullable[symbol]);
        }

        /// <summary>
        /// Can a given symbol (typically nonterminal) derive the empty string such that the nonterminal can be erased.
        /// </summary>
        /// <param name="symbol">The (typically nonterminal) symbol.</param>
        /// <returns>True, if the (typically nonterminal) is nullable (such that the nonterminal symbol is erasable).</returns>
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public bool NULLABLE(Symbol symbol)
        {
            return Nullable[symbol];
        }

        // FIRST(u) is the set of terminals that can occur first in a full derivation of u,
        // where u is a sequence of terminals and non-terminals.

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public IReadOnlySet<Terminal> FIRST(int productionIndex)
        {
            // cannot use Variable on LHS, because it relates to many rules
            //return First[Productions[productionIndex].Head];

            var first = new Set<Terminal>();
            var production = Productions[productionIndex];

            // For each RHS symbol in production X → Y1 Y2...Yn
            for (var i = 0; i < production.Length; i++)
            {
                // X → Y1 Y2...Yn
                Symbol Yi = production.Tail[i];

                // If all symbols Y1 Y2...Y(i-1) preceding Yi is nullable,
                if (i == 0 || production.Tail.Take(i).All(NULLABLE))
                {
                    // then add First(Yi) to First(X)
                    first.AddRange(FIRST(Yi)); // NOTE: not a recursive call
                }
            }

            return first;
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public IReadOnlySet<Terminal> FIRST(Symbol symbol)
        {
            return First[symbol];
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public IReadOnlySet<Terminal> FOLLOW(Symbol symbol)
        {
            return Follow[symbol];
        }

        // nullable extended to all grammar symbols, including epsilon
        private Dictionary<Symbol, bool> _nullable;
        protected Dictionary<Symbol, bool> Nullable
        {
            get
            {
                if (_nullable == null || _fixedPointVersion != _version)
                    ComputeNullableAndFirstAndFollow();

                return _nullable;
            }
        }

        // first extended to all grammar symbols, including epsilon
        private Dictionary<Symbol, Set<Terminal>> _first;
        protected Dictionary<Symbol, Set<Terminal>> First
        {
            get
            {
                if (_first == null || _fixedPointVersion != _version)
                    ComputeNullableAndFirstAndFollow();

                return _first;
            }
        }

        private Dictionary<Symbol, Set<Terminal>> _follow;
        protected Dictionary<Symbol, Set<Terminal>> Follow
        {
            get
            {
                if (_follow == null || _fixedPointVersion != _version)
                    ComputeNullableAndFirstAndFollow();

                return _follow;
            }
        }

        // =============================================================================
        // Fixed-point problems can (sometimes) be solved using iteration:
        // Guess an initial value x0, then apply the function iteratively, until the
        // fixed point is reached:
        //
        //      x(1) := f(x(0))
        //      x(2) := f(x(1))
        //          .
        //          .
        //          .
        //      x(n) := f(x(n-1))
        //
        // until x(n) == x(n-1)
        // This is called a fixed-point iteration, and x(n) is the fixed point.
        // =============================================================================
        private Dictionary<Symbol, bool> ComputeNullable()
        {
            // extend nullable to all grammar symbols, including epsilon (to avoid need
            // for symbol.IsEpsilon checks), and initialize all values to false (except epsilon)
            var nlblMap = AllSymbols.ToDictionary(symbol => symbol, symbol => symbol.IsEpsilon);

            bool changed = true;
            while (changed)
            {
                changed = false;
                // For each production X → Y1 Y2...Yn
                foreach (var production in Productions)
                {
                    if (!nlblMap[production.Head])
                    {
                        // if all symbols Y1 Y2...Yn are nullable (e.g. if X is an ε-production)
                        if (production.Tail.All(symbol => nlblMap[symbol]))
                        {
                            nlblMap[production.Head] = true;
                            changed = true;
                        }
                    }
                }
            }

            return nlblMap;
        }

        // From the dragon book
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
        // if for all i=1,…,n, First(Xi) contains ε, then First (α) contains ε.
        //=======================================================================================================
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public void ComputeNullableAndFirstAndFollow()
        {
            // we keep a separate nullable map, instead of adding epsilon to First sets
            var nullableMap = AllSymbols.ToDictionary(symbol => symbol, symbol => symbol.IsEpsilon);
            var firstMap = AllSymbols.ToDictionary(symbol => symbol, _ => new Set<Terminal>());
            var followMap = AllSymbols.ToDictionary(symbol => symbol, _ => new Set<Terminal>());

            // Base case: First(a) = {a} for all terminal symbols a in T.
            foreach (var symbol in Terminals)
                firstMap[symbol].Add(symbol);

            // We only need to place Eof ('$' in the dragon book) in FOLLOW(S) if the grammar haven't
            // already been extended with a new nonterminal start symbol S' and a production S' -> S$ in P.
            if (!IsAugmentedWithEofMarker)
                followMap[StartSymbol].Add(Symbol.Eof);

            bool changed = true;
            while (changed)
            {
                changed = false;
                // For each production X → Y1 Y2...Yn
                foreach (var production in Productions)
                {
                    if (!nullableMap[production.Head])
                    {
                        // if all symbols Y1 Y2...Yn are nullable (e.g. if X is an ε-production)
                        if (production.Tail.All(symbol => nullableMap[symbol]))
                        {
                            nullableMap[production.Head] = true;
                            changed = true;
                        }
                    }

                    // For each RHS symbol in production X → Y1 Y2...Yn
                    for (var i = 0; i < production.Length; i++)
                    {
                        // X → Y1 Y2...Yn
                        Symbol Yi = production.Tail[i];

                        // If all symbols Y1 Y2...Y(i-1) preceding Yi are nullable,
                        if (i == 0 || production.Tail.Take(i).All(symbol => nullableMap[symbol]))
                        {
                            // then everything in First(Yi) is in First(X)
                            if (firstMap[production.Head].AddRange(firstMap[Yi]))
                                changed = true;
                        }

                        // If all symbols Y(i+1)...Yn succeeding Yi are nullable,
                        if (i == production.Tail.Count - 1 || production.Tail.Skip(i + 1).All(symbol => nullableMap[symbol]))
                        {
                            // then everything in Follow(X) is in Follow(Yi)
                            if (followMap[Yi].AddRange(followMap[production.Head]))
                                changed = true;
                        }

                        // Here we try to add everything in First(Y(i+1)...Yn) to Follow(Yi)
                        // For each symbol Y(j) in Y(i+1)...Yn, e.g. for each Y(j) succeeding Y(i)
                        for (var j = i + 1; j < production.Tail.Count; j++)
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

            _nullable = nullableMap;
            _first = firstMap;
            _follow = followMap;
            _fixedPointVersion = _version;
        }

        private IEnumerable<ProductionItem> GetClosureItemsOf(Symbol variable)
        {
            if (!(variable is NonTerminal))
            {
                yield break;
            }

            // only variables (non-terminals) have closure items
            for (int i = 0; i < Productions.Count; i++)
            {
                if (Productions[i].Head.Equals(variable))
                {
                    yield return new ProductionItem(Productions[i], i, 0);
                }
            }
        }

        // LR(0) Automaton is a DFA that we use to recognize the viable prefix/handles in the grammar
        // The machine can be generated in two different ways:
        //      NFA -> DFA in 2 passes (steps)
        //          Step 1 (NFA GOTO):
        //              NFA, where each state is an item in the canonical collection
        //              of LR(0) items (ProductionItem instances are the NFA states)
        //          Step 2 (DFA CLOSURE)
        //              Subset construction creates the canonical collection of *sets of*
        //              LR(0) items (ProductionItemSet instances are the DFA states)
        //      DFA in single-pass:
        //          Dragon book algorithm (using GOTO and CLOSURE together)
        public Nfa<ProductionItem, Symbol> GetCharacteristicStringsNfa()
        {
            if (Productions.Count == 0)
            {
                throw new InvalidOperationException("The grammar has no productions.");
            }

            if (!IsAugmented)
            {
                throw new InvalidOperationException("The grammar should be augmented with canonical S' -> S production.");
            }

            if (!IsReduced)
            {
                throw new InvalidOperationException("The grammar contains useless symbols.");
            }

            var startItem = new ProductionItem(Productions[0], 0, 0);
            var transitions = new List<Transition<Symbol, ProductionItem>>();
            var acceptItems = new List<ProductionItem>();

            // (a) For every terminal a in T, if A → α"."aβ is a marked production, then
            //     there is a transition on input a from state A → α"."aβ to state A → αa"."β
            //     obtained by "shifting the dot"
            // (b) For every variable B in V, if A → α"."Bβ is a marked production, then
            //     there is a transition on input B from state A → α"."Bβ to state A → αB"."β
            //     obtained by "shifting the dot", and transitions on input ε (the empty string)
            //     to all states B → "."γ(i), for all productions B → γ(i) in P with left-hand side B.
            int productionIndex = 0;
            foreach (var production in Productions)
            {
                for (int dotPosition = 0; dotPosition <= production.Tail.Count; dotPosition += 1)
                {
                    // (productionIndex, dotPosition) is identifier
                    var item = new ProductionItem(production, productionIndex, dotPosition);

                    // (a) A → α"."aβ
                    if (item.IsShiftItem)
                    {
                        Symbol label = item.GetNextSymbol<Terminal>();
                        var shiftToItem = item.GetNextItem();
                        transitions.Add(Transition.Move(item, label, shiftToItem));
                    }

                    // (b) A → α"."Bβ
                    if (item.IsGotoItem)
                    {
                        Symbol nonTerminal = item.GetNextSymbol<NonTerminal>();
                        var goToItem = item.GetNextItem();
                        transitions.Add(Transition.Move(item, nonTerminal, goToItem));

                        // closure items
                        foreach (var closureItem in GetClosureItemsOf(nonTerminal))
                        {
                            // Expecting to see a non terminal 'B' is the same as expecting to see
                            // RHS grammar symbols 'γ(i)', where B → γ(i) is a production in P
                           transitions.Add(Transition.EpsilonMove<Symbol, ProductionItem>(item, closureItem));
                        }
                    }

                    // (c) A → β"." (Accepting states has dot shifted all the way to the end)
                    if (item.IsReduceItem)
                    {
                        acceptItems.Add(item);
                    }
                }

                productionIndex += 1;
            }

            return new Nfa<ProductionItem, Symbol>(transitions, startItem, acceptItems);
        }

        public IEnumerator<Production> GetEnumerator()
        {
            return Productions.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override string ToString()
        {
            return Productions
                .Aggregate((i: 0, sb: new StringBuilder()), (t, p) => (t.i + 1, t.sb.AppendLine($"{t.i}: {p}"))).sb
                .ToString();
        }
    }

    public static class UtilityExtensions
    {
        public static IEnumerable<T> AsSingletonEnumerable<T>(this T item)
        {
            yield return item;
        }

        public static bool AddRange<T>(this HashSet<T> hashSet, IEnumerable<T> items)
        {
            var c = hashSet.Count;
            hashSet.UnionWith(items);
            return hashSet.Count > c;
        }

        public static IEnumerable<Terminal> WithEofMarker(this IEnumerable<Terminal> terminalSymbols)
        {
            return terminalSymbols.Concat(Symbol.Eof.AsSingletonEnumerable());
        }

        public static void Each<T>(this IEnumerable<T> e, Action<T> a)
        {
            foreach (var i in e) a(i);
        }
    }
}
