using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using AutomataLib;

namespace ContextFreeGrammar
{
    /// <summary>
    /// Simple textbook grammar, where tokens are single character letters
    /// </summary>
    public class Grammar : Grammar<Nonterminal, Terminal>
    {
        public Grammar(IEnumerable<Nonterminal> variables, IEnumerable<Terminal> terminals, Nonterminal startSymbol)
            : base(variables, terminals, startSymbol)
        {
        }
    }

    // First and Follow functions associated with a grammar G is important when conducting LL and
    // LR (SLR(1), LR(1), LALR(1)) parser, because the setting up of parsing table is aided by them

    // If α is any string of grammar symbols, let First(α) be the set of terminals that begin the
    // strings derived from α, if α *=> ε, then ε is also in First(α).

    // Define Follow(A), for non-terminal A, to be the set of terminals a that can appear immediately
    // to the right of A in some sentential form. That is, the set of terminals a such that there
    // exists a derivation of the form S *=> αAaβ for some α and β. Note that there may, at some time
    // during the derivation, have been symbols between A and a, but if so, they derived ε and disappeared.

    /// <summary>
    /// Context-free grammar (CFG)
    /// </summary>
    public class Grammar<TNonterminalSymbol, TTerminalSymbol> : IProductionsContainer<TNonterminalSymbol>, IEnumerable<Production<TNonterminalSymbol>>
        where TNonterminalSymbol : Symbol, IEquatable<TNonterminalSymbol>
        where TTerminalSymbol : Symbol, IEquatable<TTerminalSymbol>
    {
        private readonly List<Production<TNonterminalSymbol>> _productions;
        private readonly Dictionary<TNonterminalSymbol, List<(int, Production<TNonterminalSymbol>)>> _productionMap;

        private int _version;
        private int _fixedPointVersion;

        public Grammar(IEnumerable<TNonterminalSymbol> variables, IEnumerable<TTerminalSymbol> terminals, TNonterminalSymbol startSymbol)
        {
            _productions = new List<Production<TNonterminalSymbol>>();              // insertion ordered list
            Variables = new InsertionOrderedSet<TNonterminalSymbol>(variables);     // insertion ordered set
            Terminals = new Set<TTerminalSymbol>(terminals);                        // unordered set
            StartSymbol = startSymbol;
            _productionMap = Variables.ToDictionary(symbol => symbol, _ => new List<(int, Production<TNonterminalSymbol>)>());
        }

        /// <summary>
        /// First production is a unit production (S → E), where the head variable (S) has only this single production,
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
        public IReadOnlyOrderedSet<TNonterminalSymbol> Variables { get; }

        public IEnumerable<Symbol> NonTerminalSymbols => Variables;

        /// <summary>
        /// Terminal grammar symbols, not including ε (the empty string)
        /// </summary>
        public IReadOnlySet<TTerminalSymbol> Terminals { get; }

        public IEnumerable<Symbol> TerminalSymbols => Terminals;

        /// <summary>
        /// All grammar symbols (terminal and nonterminal symbols), not including ε (the empty string).
        /// </summary>
        public IEnumerable<Symbol> Symbols => NonTerminalSymbols.Concat(TerminalSymbols);

        /// <summary>
        /// All grammar symbols (terminal and nonterminal symbols), including ε (the empty string).
        /// </summary>
        public IEnumerable<Symbol> AllSymbols => Symbols.Concat(Symbol.Epsilon.AsSingletonEnumerable());

        /// <inheritdoc />
        public IReadOnlyList<Production<TNonterminalSymbol>> Productions => _productions;

        ///// <summary>
        ///// Production rules for any given variable (nonterminal symbol).
        ///// </summary>
        //public IReadOnlyDictionary<Nonterminal, IReadOnlyList<Production>> ProductionsFor =>
        //    (IReadOnlyDictionary<Nonterminal, IReadOnlyList<Production>>) _productionMap;

        /// <summary>
        /// The start symbol.
        /// </summary>
        public TNonterminalSymbol StartSymbol { get; }

        public void Add(Production<TNonterminalSymbol> production)
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

            int index = _productions.Count;
            _productions.Add(production);
            _productionMap[production.Head].Add((index, production));
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
        public IReadOnlySet<TTerminalSymbol> FIRST(int productionIndex)
        {
            // cannot use Variable on LHS, because it relates to many rules
            //return First[Productions[productionIndex].Head];

            var first = new Set<TTerminalSymbol>();
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
        public IReadOnlySet<TTerminalSymbol> FIRST(Symbol symbol)
        {
            return First[symbol];
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public IReadOnlySet<TTerminalSymbol> FOLLOW(TNonterminalSymbol symbol)
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
        private Dictionary<Symbol, Set<TTerminalSymbol>> _first;
        protected Dictionary<Symbol, Set<TTerminalSymbol>> First
        {
            get
            {
                if (_first == null || _fixedPointVersion != _version)
                    ComputeNullableAndFirstAndFollow();

                return _first;
            }
        }

        private Dictionary<TNonterminalSymbol, Set<TTerminalSymbol>> _follow;
        protected Dictionary<TNonterminalSymbol, Set<TTerminalSymbol>> Follow
        {
            get
            {
                if (_follow == null || _fixedPointVersion != _version)
                    ComputeNullableAndFirstAndFollow();

                return _follow;
            }
        }

        // This method is kept around, because we might need to calculate nullable predicate, if calculating
        // FIRST and FOLLOW sets using a Graph representing all the recursive set constraints as a relation and
        // using Graph traversal as an efficient iteration technique to solve for the unique least fixed-point solution.
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
        private (Dictionary<Symbol, bool>, Dictionary<Symbol, Set<TTerminalSymbol>>) ComputeFirst()
        {
            // We extend Nullable and First to the entire vocabulary of grammar symbols, including epsilon
            var nullableMap = ComputeNullable();
            var firstMap = AllSymbols.ToDictionary(symbol => symbol, _ => new Set<TTerminalSymbol>());

            // Base case: First(a) = {a} for all terminal symbols a in T.
            foreach (var terminal in Terminals)
                firstMap[terminal].Add(terminal);

            // Simple brute-force Fixed-Point Iteration inspired by Dragon Book
            bool changed = true;
            while (changed)
            {
                changed = false;
                // For each production X → Y1 Y2...Yn
                foreach (var production in Productions)
                {
                    // FirstDelta(X) = FIRST(Y1) U ... U FIRST(Yk), where k, 1 <= k <= n, is
                    // the largest integer such that Nullable(Y1) = ... = Nullable(Yk) = true
                    // is added to First(X)
                    for (int i = 0; i < production.Length; i++)
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

        // Also inspired by Dragon book....should be changed to more clever Graph Traversal Method
        // See also https://compilers.iecc.com/comparch/article/01-04-079 for sketch of algorithm
        // based on set-valued functions over digraph containing relations/edges for all set constraints
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public (Dictionary<Symbol, bool>,
                Dictionary<Symbol, Set<TTerminalSymbol>>,
                Dictionary<TNonterminalSymbol, Set<TTerminalSymbol>>) ComputeFollow()
        {
            var (nullableMap, firstMap) = ComputeFirst();

            // Why extend Follow to all symbols??? shouldn't it be defined only for terminal symbols???
            var followMap = Variables.ToDictionary(symbol => symbol, _ => new Set<TTerminalSymbol>());

            // We only need to place Eof ('$' in the dragon book) in FOLLOW(S) if the grammar haven't
            // already been extended with a new nonterminal start symbol S' and a production S' → S$ in P.
            if (!IsAugmentedWithEofMarker)
                followMap[StartSymbol].Add(Symbol.Eof<TTerminalSymbol>());

            // Simple brute-force Fixed-Point Iteration inspired by Dragon Book
            bool changed = true;
            while (changed)
            {
                changed = false;
                // For each production X → Y1 Y2...Yn
                foreach (var production in Productions)
                {
                    for (int i = 0; i < production.Length; i++)
                    {
                        // for each Yi that is a nonterminal symbol
                        var Yi = production.TailAs<TNonterminalSymbol>(i);
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
            Set<TTerminalSymbol> First(IEnumerable<Symbol> symbols)
            {
                var m = new Set<TTerminalSymbol>();
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

        public void ComputeNullableAndFirstAndFollow()
        {
            var (nullableMap, firstMap, followMap) = ComputeFollow();
            _nullable = nullableMap;
            _first = firstMap;
            _follow = followMap;
            _fixedPointVersion = _version;
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public void OldComputeNullableAndFirstAndFollow()
        {
            // we keep a separate nullable map, instead of adding epsilon to First sets
            var nullableMap = AllSymbols.ToDictionary(symbol => symbol, symbol => symbol.IsEpsilon);
            var firstMap = AllSymbols.ToDictionary(symbol => symbol, _ => new Set<TTerminalSymbol>());
            var followMap = Variables.ToDictionary(symbol => symbol, _ => new Set<TTerminalSymbol>());

            // Base case: First(a) = {a} for all terminal symbols a in T.
            foreach (var symbol in Terminals)
                firstMap[symbol].Add(symbol);

            // We only need to place Eof ('$' in the dragon book) in FOLLOW(S) if the grammar haven't
            // already been extended with a new nonterminal start symbol S' and a production S' → S$ in P.
            if (!IsAugmentedWithEofMarker)
                followMap[StartSymbol].Add(Symbol.Eof<TTerminalSymbol>());

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
                        Symbol yi = production.Tail[i]; // FIRST is extended to all symbols

                        // If all symbols Y1 Y2...Y(i-1) preceding Yi are nullable,
                        if (i == 0 || production.Tail.Take(i).All(symbol => nullableMap[symbol]))
                        {
                            // then everything in First(Yi) is in First(X)
                            if (firstMap[production.Head].AddRange(firstMap[yi]))
                                changed = true;
                        }

                        // for each Yi that is a nonterminal symbol
                        var Yi = yi as TNonterminalSymbol; // FOLLOW is only defined w.r.t. nonterminal symbols.
                        if (Yi == null) continue;

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

        /// <summary>
        /// Get NFA representation of the set of characteristic strings (aka viable prefixes) that are defined by
        /// CG = {αβ ∈ Pow(V) | S′ ∗⇒ αAv ⇒ αβv, αβ ∈ Pow(V), v ∈ Pow(T)}, where V := N U V (all grammar symbols),
        /// and ⇒ is the right-most derivation relation. CG is the set of viable prefixes containing all prefixes (αβ)
        /// of right sentential forms (αβv) that can appear on the stack of a shift/reduce parser,
        /// i.e. prefixes of right sentential forms that do not extend past the end of the right-most handle
        /// (A handle, β, of a right sentential form, αβv, is a production, A → β, and a position within the
        /// right sentential form where the substring β can be found).
        /// </summary>
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public Nfa<ProductionItem<TNonterminalSymbol>, Symbol> GetLr0AutomatonNfa()
        {
            // NOTE: These are all synonyms for what machine we are building here
            //          - 'characteristic strings' recognizer
            //          - 'viable prefix' recognizer
            //          - 'handle' recognizer
            //          - LR(0) automaton

            if (Productions.Count == 0)
            {
                throw new InvalidOperationException("The grammar has no productions.");
            }

            if (!IsAugmented)
            {
                throw new InvalidOperationException("The grammar should be augmented with canonical S' → S production.");
            }

            if (!IsReduced)
            {
                throw new InvalidOperationException("The grammar contains useless symbols.");
            }

            var startItem = new ProductionItem<TNonterminalSymbol>(Productions[0], 0, 0);
            var transitions = new List<Transition<Symbol, ProductionItem<TNonterminalSymbol>>>();
            var acceptItems = new List<ProductionItem<TNonterminalSymbol>>();

            // (a) For every terminal a in T, if A → α•aβ is a marked production, then
            //     there is a transition on input a from state A → α•aβ to state A → αa•β
            //     obtained by "shifting the dot"
            // (b) For every variable B in V, if A → α•Bβ is a marked production, then
            //     there is a transition on input B from state A → α•Bβ to state A → αB•β
            //     obtained by "shifting the dot", and transitions on input ε (the empty string)
            //     to all states B → •γ(i), for all productions B → γ(i) in P with left-hand side B.
            int productionIndex = 0;
            foreach (var production in Productions)
            {
                for (int dotPosition = 0; dotPosition <= production.Tail.Count; dotPosition += 1)
                {
                    // (productionIndex, dotPosition) is identifier
                    var item = new ProductionItem<TNonterminalSymbol>(production, productionIndex, dotPosition);

                    // (a) A → α•aβ
                    if (item.IsShiftItem)
                    {
                        Symbol a = item.GetNextSymbol<Terminal>();
                        var shiftToItem = item.GetNextItem();
                        transitions.Add(Transition.Move(item, a, shiftToItem));
                    }

                    // (b) A → α•Bβ
                    if (item.IsGotoItem)
                    {
                        var B = item.GetNextSymbol<TNonterminalSymbol>();
                        var goToItem = item.GetNextItem();
                        transitions.Add(Transition.Move(item, (Symbol)B, goToItem));

                        // closure items
                        foreach (var (index, productionOfB) in _productionMap[B])
                        {
                            var closureItem = new ProductionItem<TNonterminalSymbol>(productionOfB, index, 0);
                            // Expecting to see a nonterminal 'B' is the same as expecting to see
                            // RHS grammar symbols 'γ(i)', where B → γ(i) is a production in P
                            transitions.Add(Transition.EpsilonMove<Symbol, ProductionItem<TNonterminalSymbol>>(item, closureItem));
                        }
                    }

                    // (c) A → β• (Accepting states has dot shifted all the way to the end)
                    if (item.IsReduceItem)
                    {
                        acceptItems.Add(item);
                    }
                }

                productionIndex += 1;
            }

            return new Nfa<ProductionItem<TNonterminalSymbol>, Symbol>(transitions, startItem, acceptItems);
        }

        /// <summary>
        /// Get DFA representation of the set of characteristic strings (aka viable prefixes) that are defined by
        /// CG = {αβ ∈ Pow(V) | S′ ∗⇒ αAv ⇒ αβv, αβ ∈ Pow(V), v ∈ Pow(T)}, where V := N U V (all grammar symbols),
        /// and ⇒ is the right-most derivation relation. CG is the set of viable prefixes containing all prefixes (αβ)
        /// of right sentential forms (αβv) that can appear on the stack of a shift/reduce parser,
        /// i.e. prefixes of right sentential forms that do not extend past the end of the right-most handle
        /// (A handle, β, of a right sentential form, αβv, is a production, A → β, and a position within the
        /// right sentential form where the substring β can be found).
        /// </summary>
        public Dfa<ProductionItemSet<TNonterminalSymbol>, Symbol> GetLr0AutomatonDfa()
        {
            // TODO: GetLr0AutomatonDfa and ComputeSlrParsingTable have the same routine for building the
            //          canonical LR(0) collection of LR(0) item sets (states)
            //          transitions
            var (states, transitions) = ComputeLr0AutomatonData();

            var acceptStates = states.Where(itemSet => itemSet.ReduceItems.Any()).ToList();

            // NOTE: This DFA representation always need to have a so called dead state (0),
            // and {1,2,...,N} are therefore the integer values of the actual states.
            return new Dfa<ProductionItemSet<TNonterminalSymbol>, Symbol>(states, Symbols, transitions, states[0], acceptStates);
        }

        /// <summary>
        /// Compute LR(0) parsing table.
        /// </summary>
        public LrParser<TNonterminalSymbol, TTerminalSymbol> ComputeLr0ParsingTable()
        {
            var (states, transitions) = ComputeLr0AutomatonData();

            // LR(0)
            var (actionTableEntries, gotoTableEntries) = ComputeParsingTableData(states, transitions, _ => Terminals);

            // NOTE: The ParsingTable representation does not have a dead state (not required), and therefore states
            // are given by {0,1,...,N-1}.
            return new LrParser<TNonterminalSymbol, TTerminalSymbol>(this, states, Variables, Terminals,
                actionTableEntries, gotoTableEntries);
        }

        /// <summary>
        /// Compute SLR(1) parsing table.
        /// </summary>
        public LrParser<TNonterminalSymbol, TTerminalSymbol> ComputeSlrParsingTable()
        {
            var (states, transitions) = ComputeLr0AutomatonData();

            // SLR(1)
            var (actionTableEntries, gotoTableEntries) = ComputeParsingTableData(states, transitions, reduceByRule => FOLLOW(reduceByRule.Head));

            // NOTE: The ParsingTable representation does not have a dead state (not required), and therefore states
            // are given by {0,1,...,N-1}.
            return new LrParser<TNonterminalSymbol, TTerminalSymbol>(this, states, Variables, Terminals,
                actionTableEntries, gotoTableEntries);
        }

        /// <summary>
        /// Translate LR(0) automaton representation into a shift-reduce parsing table.
        /// </summary>
        /// <param name="states">The canonical LR(0) collection of LR(0) item sets.</param>
        /// <param name="transitions">The transitions of the LR(0) automaton (GOTO successor function in dragon book)</param>
        /// <param name="reduceOnTerminalSymbols"></param>
        /// <returns></returns>
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private (IEnumerable<LrActionEntry<TTerminalSymbol>>, IEnumerable<LrGotoEntry<TNonterminalSymbol>>) ComputeParsingTableData(
            IReadOnlyOrderedSet<ProductionItemSet<TNonterminalSymbol>> states,
            List<Transition<Symbol, ProductionItemSet<TNonterminalSymbol>>> transitions,
            Func<Production<TNonterminalSymbol>, IEnumerable<TTerminalSymbol>> reduceOnTerminalSymbols
            )
        {
            var actionTableEntries = new List<LrActionEntry<TTerminalSymbol>>();
            var gotoTableEntries = new List<LrGotoEntry<TNonterminalSymbol>>();

            // renaming all LR(0) item sets to integer states
            var indexMap = new Dictionary<ProductionItemSet<TNonterminalSymbol>, int>(capacity: states.Count);
            int stateIndex = 0;
            foreach (ProductionItemSet<TNonterminalSymbol> state in states)
            {
                indexMap.Add(state, stateIndex);
                stateIndex += 1;
            }

            // NOTE: Important that shift/goto actions are inserted (configured) before
            //       reduce actions in the action table (conflict resolution).

            // Shift and Goto actions (directly from the transitions of the LR(0) automaton).
            // TODO: My guess is these are the same across all LR methods????!!!!????
            foreach (var move in transitions)
            {
                int source = indexMap[move.SourceState];
                int target = indexMap[move.TargetState];

                if (move.Label.IsTerminal)
                {
                    // If A → α•aβ is in LR(0) item set, where a is a terminal symbol
                    var a = (TTerminalSymbol) move.Label;
                    //var symbolIndex = _terminalToIndex[a];
                    //_actionTable[source, symbolIndex] = LrAction.Shift(target);
                    actionTableEntries.Add(new LrActionEntry<TTerminalSymbol>(source, a, LrAction.Shift(target)));
                }
                else
                {
                    // If A → α•Xβ is in LR(0) item set, where X is a nonterminal symbol
                    var X = (TNonterminalSymbol) move.Label;
                    //int symbolIndex = _nonterminalToIndex[X];
                    //_gotoTable[source, symbolIndex - 1] = target;
                    gotoTableEntries.Add(new LrGotoEntry<TNonterminalSymbol>(source, X, target));
                }
            }

            // Reduce actions differ between different LR methods (SLR strategy uses FOLLOW(A) below)
            // TODO: My guess is these differ between different LR methods????!!!!????
            foreach (ProductionItemSet<TNonterminalSymbol> itemSet in states)
            {
                // If A → α• is in LR(0) item set, then set action[s, a] to 'reduce A → α•' (where A is not S')
                //      for all a in T               (LR(0) table)
                //      for all a in FOLLOW(A)       (SLR(1) table)
                if (itemSet.IsReduceAction)
                {
                    // choose reductions with lowest index in reduce/reduce conflict resolution
                    foreach (ProductionItem<TNonterminalSymbol> reduceItem in
                        itemSet.ReduceItems.OrderBy(item => item.ProductionIndex))
                    {
                        var state = indexMap[itemSet];
                        // LR(0) and SLR(1) grammar rules are supported here
                        foreach (var terminal in reduceOnTerminalSymbols(reduceItem.Production))
                        {
                            var reduceAction = LrAction.Reduce(reduceItem.ProductionIndex);
                            actionTableEntries.Add(new LrActionEntry<TTerminalSymbol>(state, terminal, reduceAction));
                        }
                    }
                }

                // If S' → S• is in LR(0) item set, then set action[s, $] to accept
                if (itemSet.IsAcceptAction)
                {
                    // NOTE: Only if the grammar is augmented with S' → S$ (i.e. EOF marker added
                    // to augmented rule) then we can be sure that the accept action item set has
                    // the EOF marked symbol ($) as the spelling property.
                    //      Debug.Assert(itemSet.SpellingSymbol.Equals(Symbol.Eof<TTerminalSymbol>()));
                    actionTableEntries.Add(new LrActionEntry<TTerminalSymbol>(indexMap[itemSet],
                        Symbol.Eof<TTerminalSymbol>(), LrAction.Accept));
                }
            }

            return (actionTableEntries, gotoTableEntries);
        }

        /// <summary>
        /// Get data representation of the set of characteristic strings (aka viable prefixes) that are defined by
        /// CG = {αβ ∈ Pow(V) | S′ ∗⇒ αAv ⇒ αβv, αβ ∈ Pow(V), v ∈ Pow(T)}, where V := N U V (all grammar symbols),
        /// and ⇒ is the right-most derivation relation. CG is the set of viable prefixes containing all prefixes (αβ)
        /// of right sentential forms (αβv) that can appear on the stack of a shift/reduce parser,
        /// i.e. prefixes of right sentential forms that do not extend past the end of the right-most handle
        /// (A handle, β, of a right sentential form, αβv, is a production, A → β, and a position within the
        /// right sentential form where the substring β can be found).
        /// </summary>
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private (IReadOnlyOrderedSet<ProductionItemSet<TNonterminalSymbol>> states,
                 List<Transition<Symbol, ProductionItemSet<TNonterminalSymbol>>> transitions) ComputeLr0AutomatonData()
        {
            ProductionItemSet<TNonterminalSymbol> startItemSet =
                Closure(new ProductionItem<TNonterminalSymbol>(Productions[0], 0, 0).AsSingletonEnumerable());
            // states/items er numbered 0,1,2...in insertion order, such that the start state is always at index zero.
            var states = new InsertionOrderedSet<ProductionItemSet<TNonterminalSymbol>>(startItemSet.AsSingletonEnumerable());
            var transitions = new List<Transition<Symbol, ProductionItemSet<TNonterminalSymbol>>>();

            // work-list implementation
            var markedAddedItemSets = new Queue<ProductionItemSet<TNonterminalSymbol>>(startItemSet.AsSingletonEnumerable());
            while (markedAddedItemSets.Count > 0)
            {
                ProductionItemSet<TNonterminalSymbol> sourceState = markedAddedItemSets.Dequeue();
                // For each pair (X, { A → αX•β, where the item A → α•Xβ is in the predecessor item set}),
                // where A → αX•β is core/kernel successor item on some grammar symbol X in the graph
                foreach (var coreSuccessorItems in sourceState.GetTargetItems())
                {
                    // For each grammar symbol (label on the transition/edge in the graph)
                    var X = coreSuccessorItems.Key;
                    // Get the closure of all the core/kernel successor items A → αX•β that we can move/transition to in the graph
                    ProductionItemSet<TNonterminalSymbol> targetState = Closure(coreSuccessorItems);
                    transitions.Add(Transition.Move(sourceState, X, targetState));
                    if (!states.Contains(targetState))
                    {
                        markedAddedItemSets.Enqueue(targetState);
                        states.Add(targetState);
                    }
                }
            }

            return (states, transitions);
        }

        /// <summary>
        /// Compute ε-closure of the kernel/core items of any LR(0) item set --- this
        /// is identical to ε-closure in the subset construction algorithm when translating
        /// NFA to DFA .
        /// </summary>
        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private ProductionItemSet<TNonterminalSymbol> Closure(IEnumerable<ProductionItem<TNonterminalSymbol>> coreItems)
        {
            var closure = new HashSet<ProductionItem<TNonterminalSymbol>>(coreItems);

            // work-list implementation
            var markedAddedItems = new Queue<ProductionItem<TNonterminalSymbol>>(coreItems);
            while (markedAddedItems.Count != 0)
            {
                ProductionItem<TNonterminalSymbol> item = markedAddedItems.Dequeue();
                var B = item.GetNextSymbolAs<TNonterminalSymbol>();
                if (B == null) continue;
                // If item is a GOTO item of the form A → α•Bβ, where B is in T,
                // then find all its closure items
                foreach (var (index, production) in _productionMap[B])
                {
                    var closureItem = new ProductionItem<TNonterminalSymbol>(production, index, 0);
                    if (!closure.Contains(closureItem))
                    {
                        closure.Add(closureItem);
                        markedAddedItems.Enqueue(closureItem);
                    }
                }
            }

            return new ProductionItemSet<TNonterminalSymbol>(closure);
        }

        public IEnumerator<Production<TNonterminalSymbol>> GetEnumerator()
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
                .Aggregate((i: 0, sb: new StringBuilder()), (t, p) => (t.i + 1, t.sb.AppendLine($"{t.i}: {p}")))
                .sb.ToString();
        }
    }
}
