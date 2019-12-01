using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using AutomataLib;
using ContextFreeGrammar.Analyzers;

namespace ContextFreeGrammar
{
    // First and Follow functions associated with a grammar G is important when conducting LL and
    // LR (SLR(1), LR(1), LALR(1)) parser, because the setting up of parsing table is aided by them

    // If α is any string of grammar symbols, let First(α) be the set of terminals that begin the
    // strings derived from α, if α *=> ε, then ε is also in First(α).

    // Define Follow(A), for non-terminal A, to be the set of terminals a that can appear immediately
    // to the right of A in some sentential form. That is, the set of terminals a such that there
    // exists a derivation of the form S *=> αAaβ for some α and β. Note that there may, at some time
    // during the derivation, have been symbols between A and a, but if so, they derived ε and disappeared.

    // Test your grammar online here
    // http://smlweb.cpsc.ucalgary.ca/start.html

    /// <summary>
    /// Immutable context-free grammar (CFG) type.
    /// </summary>
    public class Grammar<TNonterminalSymbol, TTerminalSymbol> : IProductionsContainer<TNonterminalSymbol>, IFollowSymbolsAnalyzer<TNonterminalSymbol, TTerminalSymbol>
        where TNonterminalSymbol : Symbol, IEquatable<TNonterminalSymbol>
        where TTerminalSymbol : Symbol, IEquatable<TTerminalSymbol>
    {
        private readonly IFollowSymbolsAnalyzer<TNonterminalSymbol, TTerminalSymbol> _analyzer;

        public Grammar(
            IEnumerable<TNonterminalSymbol> variables,
            IEnumerable<TTerminalSymbol> terminals,
            TNonterminalSymbol startSymbol,
            IEnumerable<Production<TNonterminalSymbol>> productions,
            Func<Grammar<TNonterminalSymbol, TTerminalSymbol>, IFollowSymbolsAnalyzer<TNonterminalSymbol, TTerminalSymbol>> analyzerFactory)
        {
            if (variables == null) throw new ArgumentNullException(nameof(variables));
            if (terminals == null) throw new ArgumentNullException(nameof(terminals));
            if (productions == null) throw new ArgumentNullException(nameof(productions));

            StartSymbol = startSymbol ?? throw new ArgumentNullException(nameof(startSymbol));

            Variables = new InsertionOrderedSet<TNonterminalSymbol>(variables);
            Terminals = new Set<TTerminalSymbol>(terminals);

            // Productions are numbered 0,1,2,...,^Productions.Count
            var prods = new List<Production<TNonterminalSymbol>>();
            // Variables (productions on the shorter form (A -> α | β | ...) are numbered 0,1,...,^Variables.Count
            var productionMap = Variables.ToDictionary(symbol => symbol, _ => new List<(int, Production<TNonterminalSymbol>)>());

            int index = 0;
            foreach (var production in productions)
            {
                prods.Add(production);
                productionMap[production.Head].Add((index, production));
                index += 1;
            }

            ProductionsFor = productionMap.ToImmutableDictionary(kvp => kvp.Key,
                kvp => (IReadOnlyList<(int, Production<TNonterminalSymbol>)>) kvp.Value);

            if (prods.Count == 0)
            {
                throw new ArgumentException("The productions are empty.", nameof(productions));
            }

            Productions = prods;

            // Calculate lookahead sets (Erasable, First, Follow) using strategy provided by the caller
            _analyzer = analyzerFactory(this);
        }

        /// <summary>
        /// First production is a unit production (S → E), where the head (LHS) variable (S) and the tail (RHS symbols) is
        /// a single nonterminal symbol, and the head variable (S) is found no where else in any productions. We also accept
        /// a (pseudo) unit production on the form (S' → S$), where the start unit production have been augmented with an
        /// eof marker ($).
        /// </summary>
        public bool IsAugmented =>
            Productions[0].Head.Equals(StartSymbol) &&
            (Productions[0].LastSymbol.IsEof
                ? Productions[0].Tail.Count == 2
                : Productions[0].Tail.Count == 1) &&
            Productions[0].Tail[0].IsNonterminal &&
            Productions.Skip(1).All(p => !p.Head.Equals(StartSymbol)) &&
            Productions.All(p => !p.Tail.Contains(StartSymbol));

        /// <summary>
        /// First production is a (pseudo) unit production (S' → S$), where the head (LHS) variable (S') and the tail (RHS symbols) is
        /// a single nonterminal symbol (S), and the head variable (S') is found no where else in any productions. We call this a unit
        /// production, because the eof marker ($) is not a terminal symbol, and the rule only augments the grammar (not the syntax/language)
        /// in a way that enables a shift-reduce parser to determine 'end of input'.
        /// </summary>
        public bool IsAugmentedWithEofMarker => IsAugmented && Productions[0].LastSymbol.IsEof;

        /// <summary>
        /// This is the augmented start dotted production [S' → •S].
        /// This is also the CORE of the start item of the LR(k) automaton.
        /// The (initial) start item (with CORE [S' → •S]) is the only LR(k) item in the LR(k) automaton,
        /// that is considered a kernel item even though the dot is at the beginning of the RHS of the production.
        /// </summary>
        public MarkedProduction<TNonterminalSymbol> AugmentedStartItem =>
            new MarkedProduction<TNonterminalSymbol>(Productions[0], 0, 0);

        /// <summary>
        /// This is the augmented accept dotted production [S' → S•] (or [S' → S•$] if eof marker is used).
        /// This is also the CORE of the unique accept item (augmented final item) of the LR(k) automaton.
        /// Whenever we reduce by this rule, we know we are finished. That is, if the <see cref="AugmentedAcceptItem"/>
        /// is the current state, and the input buffer is empty, the parser will accept the input.
        /// </summary>
        /// <remarks>
        /// By convention we never shift passed the eof marker. That is the final accepting state of the parser
        /// is always S' → S•$, and not S' → S$•.
        /// </remarks>
        public MarkedProduction<TNonterminalSymbol> AugmentedAcceptItem => Productions[0].LastSymbol.IsEof
            ? new MarkedProduction<TNonterminalSymbol>(Productions[0], 0, Productions[0].Length - 1)
            : new MarkedProduction<TNonterminalSymbol>(Productions[0], 0, Productions[0].Length);

        // NOTE: Our context-free grammars are (always) reduced and augmented!!!!
        // TODO: No useless symbols (required to construct DFA of LR(0) automaton, Knuths Theorem)
        public bool IsReduced => true;

        /// <summary>
        /// The set of nonterminal symbols (aka variables) used to define the grammar. The variables
        /// are defined in the order defined by the sequence of variables passed to the
        /// <see cref="Grammar{TNonterminalSymbol, TTerminalSymbol}"/> constructor.
        /// </summary>
        public IReadOnlyOrderedSet<TNonterminalSymbol> Variables { get; }

        public IEnumerable<Symbol> NonTerminalSymbols => Variables;

        /// <summary>
        /// The set of input symbols used to define the grammar.
        /// </summary>
        /// <remarks>
        /// If the grammar is augmented with an eof marker symbol, the <see cref="Symbol.EofMarker"/> is
        /// included in the <see cref="Terminals"/> set.
        /// </remarks>
        public IReadOnlySet<TTerminalSymbol> Terminals { get; }

        public IEnumerable<Symbol> TerminalSymbols => Terminals;

        /// <summary>
        /// All grammar symbols (terminal and nonterminal symbols), not including ε (the empty string).
        /// </summary>
        public IEnumerable<Symbol> Symbols => NonTerminalSymbols.Concat(TerminalSymbols);

        /// <summary>
        /// All grammar symbols (terminal and nonterminal symbols), including ε (the empty string).
        /// </summary>
        public IEnumerable<Symbol> AllSymbols => Symbols.ConcatItem(Symbol.Epsilon);

        /// <inheritdoc />
        public IReadOnlyList<Production<TNonterminalSymbol>> Productions { get; }

        /// <summary>
        /// List of production rules for any given variable (nonterminal symbol).
        /// When A → α | β | ... | ω, then ProductionsFor[A] = α, β,..., ω.
        /// </summary>
        public IReadOnlyDictionary<TNonterminalSymbol, IReadOnlyList<(int, Production<TNonterminalSymbol>)>> ProductionsFor { get; }

        public TNonterminalSymbol StartSymbol { get; }

        public bool Erasable(int productionIndex)
        {
            return this.Erasable(Productions[productionIndex].Tail);
        }

        public IReadOnlySet<TTerminalSymbol> First(int productionIndex)
        {
            return this.First(Productions[productionIndex].Tail);
        }

        public bool Erasable(Symbol symbol) => _analyzer.Erasable(symbol);

        public IReadOnlySet<TTerminalSymbol> First(Symbol symbol) => _analyzer.First(symbol);

        public IReadOnlySet<TTerminalSymbol> Follow(TNonterminalSymbol variable) => _analyzer.Follow(variable);

        /// <summary>
        /// Get NFA representation of the set of characteristic strings (aka viable prefixes) that are defined by
        /// CG = {αβ ∈ Pow(V) | S′ ∗⇒ αAv ⇒ αβv, αβ ∈ Pow(V), v ∈ Pow(T)}, where V := N U V (all grammar symbols),
        /// and ⇒ is the right-most derivation relation. CG is the set of viable prefixes containing all prefixes (αβ)
        /// of right sentential forms (αβv) that can appear on the stack of a shift/reduce parser,
        /// i.e. prefixes of right sentential forms that do not extend past the end of the right-most handle
        /// (A handle, β, of a right sentential form, αβv, is a production, A → β, and a position within the
        /// right sentential form where the substring β can be found).
        /// </summary>
        public Nfa<ProductionItem<TNonterminalSymbol, TTerminalSymbol>, Symbol> GetLr0AutomatonNfa() =>
            Lr0AutomatonAlgorithm.GetLr0AutomatonNfa(this);

        public Nfa<ProductionItem<TNonterminalSymbol, TTerminalSymbol>, Symbol> GetLr1AutomatonNfa() =>
            Lr1AutomatonAlgorithm.GetLr1AutomatonNfa(this);

        /// <summary>
        /// Get DFA representation of the set of characteristic strings (aka viable prefixes) that are defined by
        /// CG = {αβ ∈ Pow(V) | S′ ∗⇒ αAv ⇒ αβv, αβ ∈ Pow(V), v ∈ Pow(T)}, where V := N U V (all grammar symbols),
        /// and ⇒ is the right-most derivation relation. CG is the set of viable prefixes containing all prefixes (αβ)
        /// of right sentential forms (αβv) that can appear on the stack of a shift/reduce parser,
        /// i.e. prefixes of right sentential forms that do not extend past the end of the right-most handle
        /// (A handle, β, of a right sentential form, αβv, is a production, A → β, and a position within the
        /// right sentential form where the substring β can be found).
        /// </summary>
        public Dfa<ProductionItemSet<TNonterminalSymbol, TTerminalSymbol>, Symbol> GetLr0AutomatonDfa()
        {
            var (states, transitions) = Lr0AutomatonAlgorithm.ComputeLr0AutomatonData(this);
            return Lr0AutomatonAlgorithm.GetLr0AutomatonDfa(this, states, transitions);
        }

        public Dfa<ProductionItemSet<TNonterminalSymbol, TTerminalSymbol>, Symbol> GetLr1AutomatonDfa()
        {
            var (states, transitions) = Lr1AutomatonAlgorithm.ComputeLr1AutomatonData(this);
            return Lr1AutomatonAlgorithm.GetLr1AutomatonDfa(this, states, transitions);
        }

        /// <summary>
        /// Compute LR(0) parsing table.
        /// </summary>
        public LrParser<TNonterminalSymbol, TTerminalSymbol> ComputeLr0ParsingTable()
        {
            var (states, transitions) = Lr0AutomatonAlgorithm.ComputeLr0AutomatonData(this);

            // LR(0)
            var (actionTableEntries, gotoTableEntries) = ComputeParsingTableData(states, transitions,
                (_,__) => Terminals.UnionEofMarker());

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
            var (states, transitions) = Lr0AutomatonAlgorithm.ComputeLr0AutomatonData(this);

            // SLR(1)
            var (actionTableEntries, gotoTableEntries) = ComputeParsingTableData(states, transitions,
                (_, productionIndex) => Follow(Productions[productionIndex].Head));

            // NOTE: The ParsingTable representation does not have a dead state (not required), and therefore states
            // are given by {0,1,...,N-1}.
            return new LrParser<TNonterminalSymbol, TTerminalSymbol>(this, states, Variables, Terminals,
                actionTableEntries, gotoTableEntries);
        }

        /// <summary>
        /// Compute LR(1) parsing table.
        /// </summary>
        public LrParser<TNonterminalSymbol, TTerminalSymbol> ComputeLr1ParsingTable()
        {
            var (states, transitions) = Lr1AutomatonAlgorithm.ComputeLr1AutomatonData(this);

            // LR(1)
            var (actionTableEntries, gotoTableEntries) = ComputeParsingTableData(states, transitions,
                (stateIndex, productionIndex) => states[stateIndex].ReduceBy(productionIndex).Lookaheads);

            // NOTE: The ParsingTable representation does not have a dead state (not required), and therefore states
            // are given by {0,1,...,N-1}.
            return new LrParser<TNonterminalSymbol, TTerminalSymbol>(this, states, Variables, Terminals,
                actionTableEntries, gotoTableEntries);
        }

        /// <summary>
        /// Compute LALR(1) parsing table (by 'brute force' algorithm based on merging LR(1) item sets with identical
        /// kernel items in the LR(1) automaton).
        /// </summary>
        public LrParser<TNonterminalSymbol, TTerminalSymbol> ComputeLalrParsingTable()
        {
            var (states, transitions) = Lr1AutomatonAlgorithm.ComputeLr1AutomatonData(this);

            // Merge states and transitions
            var (mergedStates, mergedTransitions) = ComputeMergedLr1AutomatonData(states, transitions);

            // LALR(1)
            var (actionTableEntries, gotoTableEntries) = ComputeParsingTableData(mergedStates, mergedTransitions,
                (stateIndex, productionIndex) => mergedStates[stateIndex].ReduceBy(productionIndex).Lookaheads);

            // NOTE: The ParsingTable representation does not have a dead state (not required), and therefore states
            // are given by {0,1,...,N-1}.
            return new LrParser<TNonterminalSymbol, TTerminalSymbol>(this, mergedStates, Variables, Terminals,
                actionTableEntries, gotoTableEntries);
        }

        /// <summary>
        /// Compute LALR(1) parsing table (by efficient digraph algorithm that simulates the valid lookahead sets in the LR(0) automaton).
        /// </summary>
        public LrParser<TNonterminalSymbol, TTerminalSymbol> ComputeEfficientLalr1ParsingTable()
        {
            var (states, transitions) = Lr0AutomatonAlgorithm.ComputeLr0AutomatonData(this);

            var dfaLr0 = Lr0AutomatonAlgorithm.GetLr0AutomatonDfa(this, states, transitions);

            var analyzer = new Lr0AutomatonDigraphAnalyzer<TNonterminalSymbol, TTerminalSymbol>(this, dfaLr0, _analyzer);

            // LALR(1)
            var (actionTableEntries, gotoTableEntries) = ComputeParsingTableData(states, transitions,
                (stateIndex, productionIndex) => analyzer.Lookaheads(stateIndex, productionIndex));

            // NOTE: The ParsingTable representation does not have a dead state (not required), and therefore states
            // are given by {0,1,...,N-1}.
            return new LrParser<TNonterminalSymbol, TTerminalSymbol>(this, states, Variables, Terminals,
                actionTableEntries, gotoTableEntries);
        }

        /// <summary>
        /// Translate LR(k) automaton (data) into a shift-reduce parsers ACTION and GOTO table entries.
        /// </summary>
        /// <param name="states">The canonical LR(0) collection of LR(0) item sets.</param>
        /// <param name="transitions">The transitions of the LR(0) automaton (GOTO successor function in dragon book)</param>
        /// <param name="reduceOnTerminalSymbols">
        /// Lambda to compute the set of valid lookahead terminal symbols of a completed (reduce) item --- the parser
        /// will perform a reduction of the recognized handle of the reduce item, if the lookahead token belongs to the computed set.
        /// The lambda will compute the lookahead set based on the state of the LR(k) automaton, and the production index of the reduction.
        /// </param>
        /// <returns>The entries of the ACTION and GOTO tables of a shift-reduce parser.</returns>
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private (IEnumerable<LrActionEntry<TTerminalSymbol>>, IEnumerable<LrGotoEntry<TNonterminalSymbol>>) ComputeParsingTableData(
            IReadOnlyOrderedSet<ProductionItemSet<TNonterminalSymbol, TTerminalSymbol>> states,
            List<Transition<Symbol, ProductionItemSet<TNonterminalSymbol, TTerminalSymbol>>> transitions,
            Func<int, int, IEnumerable<TTerminalSymbol>> reduceOnTerminalSymbols
            )
        {
            var actionTableEntries = new List<LrActionEntry<TTerminalSymbol>>();
            var gotoTableEntries = new List<LrGotoEntry<TNonterminalSymbol>>();

            // NOTE: Important that shift/goto actions are inserted (configured) before
            //       reduce actions in the action table (conflict resolution).

            // Shift and Goto actions (directly from the transitions of the LR(k) automaton).
            // In case of LR(0) automaton, the shift/goto actions are not up for debate. Only the
            // reduce actions can be configured differently depending on the method:
            //          LR(0)     -- reduce for all lookahead symbols (i.e. no lookahead)
            //          SLR(1)    -- reduce for LA(A → β•) = Follow(β)
            //          LALR(1)   -- reduce for LA(q, A → β•) = LA(q,A) = U { Follow(p,A) | in all 'lookback' states (p,A) of (q, A → β•) }
            foreach (var move in transitions)
            {
                int source = states.IndexOf(move.SourceState);
                int target = states.IndexOf(move.TargetState);

                if (move.Label.IsTerminal)
                {
                    // If [A → α•aβ, L] is in LR(k) item set, where a is a terminal symbol, and L is an arbitrary
                    // lookahead set (possibly the empty set corresponding to an LR(0) item set)
                    var a = (TTerminalSymbol)move.Label;
                    // Action[source, a] = shift target
                    actionTableEntries.Add(new LrActionEntry<TTerminalSymbol>(source, a, LrAction.Shift(target)));
                }
                else
                {
                    // If [A → α•Xβ, L] is in LR(k) item set, where X is a nonterminal symbol, and L is an arbitrary
                    // lookahead set (possibly the empty set corresponding to an LR(0) item set)
                    var X = (TNonterminalSymbol)move.Label;
                    // Goto[source, X] = target;
                    gotoTableEntries.Add(new LrGotoEntry<TNonterminalSymbol>(source, X, target));
                }
            }

            int stateIndex = 0; // states variable is an ordered set and therefore we can simulate the index without doing any lookup
            foreach (ProductionItemSet<TNonterminalSymbol, TTerminalSymbol> itemSet in states)
            {
                //  Reduce actions differ between different LR methods
                //      If a ∈ LA(s, A → α•), then set action[s, a] to 'reduce A → α•' (where A is not S')
                //          for all a ∈ T ∪ {$}            (LR(0) table of no lookahead)
                //          for all a ∈ FOLLOW(A)           (SLR(1) table with Follow set condition)
                //          for all a ∈ LA(q, A → α•)       (LALR(1) table with LR(0)-LA-Union-Pred-StateTerminal-Follow set condition)
                //          for all a ∈ L of [A → α•, L]    (LR(1) table with LR(1)-item-set-lookahead set condition)
                if (itemSet.IsReduceAction)
                {
                    // We order by production index, because we reduce with the production that comes first in
                    // the grammar specification in case of any reduce/reduce conflicts (standard conflict resolution).
                    foreach (var reduceItem in itemSet.ReduceItems)
                    {
                        foreach (var terminal in reduceOnTerminalSymbols(stateIndex, reduceItem.ProductionIndex))
                        {
                            var reduceAction = LrAction.Reduce(reduceItem.ProductionIndex);
                            actionTableEntries.Add(new LrActionEntry<TTerminalSymbol>(stateIndex, terminal, reduceAction));
                        }
                    }
                }

                // If S' → S• is in LR(0) item set, then set action[s, $] to accept
                if (itemSet.IsAcceptAction)
                {
                    actionTableEntries.Add(new LrActionEntry<TTerminalSymbol>(stateIndex,
                        Symbol.Eof<TTerminalSymbol>(), LrAction.Accept));
                }

                stateIndex += 1;
            }

            return (actionTableEntries, gotoTableEntries);
        }

        // NOTE: This is the definition of LALR(1) lookahead sets, but as an algorithm is highly inefficient.
        // NOTE: It is common for LR(1) item sets to have identical first components (i.e. identical LR(0) items),
        //       and only differ w.r.t different lookahead symbols (the second component). In construction of LALR(1) we
        //       will look for different LR(1) items having the same (kernel) LR(0) items, and merge these into new union
        //       states (i.e. new one set of items). Since the GOTO (successor) function only depends on the kernel LR(0) items
        //       of any LR(1) items, it is easy to merge the transitions of the LR(1) automaton into a new simpler LALR automaton.
        //       On the other hand the ACTION table will change, and it is possible to introduce conflicts when merging.
        private (IReadOnlyOrderedSet<ProductionItemSet<TNonterminalSymbol, TTerminalSymbol>> mergedStates,
            List<Transition<Symbol, ProductionItemSet<TNonterminalSymbol, TTerminalSymbol>>> mergedTransitions)
            ComputeMergedLr1AutomatonData(
                IReadOnlyOrderedSet<ProductionItemSet<TNonterminalSymbol, TTerminalSymbol>> states,
                List<Transition<Symbol, ProductionItemSet<TNonterminalSymbol, TTerminalSymbol>>> transitions)
        {
            // blocks[i][0] indicates what block and old index belongs to.
            // That is
            //      * blocks[i][0] == i   =>   i is itself a lower indexed block
            //      * blocks[i][0]  < i   =>   i belongs to another lower indexed block
            var blocks = new List<int>[states.Count];

            for (int i = 0; i < states.Count; i += 1)
            {
                var state = states[i];

                // Should this state be added to a lower indexed block
                for (int j = 0; j < i; j += 1)
                {
                    var lower = states[j];
                    if (state.CoreOfKernelEquals(lower))
                    {
                        // add to lower indexed block, and mark this
                        // index as added to lower indexed block.
                        blocks[j].Add(i);
                        blocks[i] = new List<int> { j };
                        break;
                    }
                }

                // if not added to lower indexed block, then index i is the lowest index of a new block
                blocks[i] = blocks[i] ?? new List<int> { i };
            }

            // Create new set of states
            var mergedStates = new InsertionOrderedSet<ProductionItemSet<TNonterminalSymbol, TTerminalSymbol>>();
            int[] oldToNew = new int[states.Count];
            for (int i = 0; i < blocks.Length; i += 1)
            {
                var state = states[i];

                if (blocks[i][0] == i)
                {
                    // Remember the new state index
                    oldToNew[i] = mergedStates.Count;

                    if (blocks[i].Count > 1)
                    {
                        // Kernel items are identical and therefore both kernel and closure items can be merged
                        // into a single item set with union lookahead sets. Create hash table with
                        // keys defined by marked productions (LR(0) items), and values defined by
                        // the union of the corresponding LR(1) items' lookahead sets.
                        Dictionary<MarkedProduction<TNonterminalSymbol>, Set<TTerminalSymbol>> itemsMap =
                            state.Items.ToDictionary(item => item.MarkedProduction, item => new Set<TTerminalSymbol>(item.Lookaheads));

                        for (int j = 1; j < blocks[i].Count; j += 1)
                        {
                            var other = states[blocks[i][j]];
                            itemsMap.MergeLookaheads(other.Items
                                .ToDictionary(item => item.MarkedProduction, item => item.Lookaheads));
                        }

                        mergedStates.Add(new ProductionItemSet<TNonterminalSymbol, TTerminalSymbol>(
                            itemsMap.Select(kvp =>
                                new ProductionItem<TNonterminalSymbol, TTerminalSymbol>(kvp.Key, kvp.Value))));
                    }
                    else
                    {
                        mergedStates.Add(state);
                    }
                }
                else
                {
                    // index i have been added to a lower indexed block, and it is therefore safe
                    // to use the oldToNew 'permutation' to translate the old index to a new index
                    int lowerIndex = blocks[i][0];
                    oldToNew[i] = oldToNew[lowerIndex];
                }
            }

            // Create new list of transition triplets (s0, label, s1) for all merged states
            var mergedTransitions =
                new List<Transition<Symbol, ProductionItemSet<TNonterminalSymbol, TTerminalSymbol>>>();
            foreach (var transition in transitions)
            {
                int oldSource = states.IndexOf(transition.SourceState);
                int oldTarget = states.IndexOf(transition.TargetState);
                // Ignore merged transitions in order to avoid duplicate moves that will result in all kinds of
                // invalid shift/shift conflicts in the parsing table (remember shift/shift conflicts are impossible)
                if (blocks[oldSource][0] < oldSource && blocks[oldTarget][0] < oldTarget)
                    continue;
                int source = oldToNew[oldSource];
                int target = oldToNew[oldTarget];
                mergedTransitions.Add(Transition.Move(mergedStates[source], transition.Label, mergedStates[target]));
            }

            return (mergedStates, mergedTransitions);
        }

        public override string ToString()
        {
            return Productions
                .Aggregate((i: 0, sb: new StringBuilder()), (t, p) => (t.i + 1, t.sb.AppendLine($"{t.i}: {p}")))
                .sb.ToString();
        }
    }
}
