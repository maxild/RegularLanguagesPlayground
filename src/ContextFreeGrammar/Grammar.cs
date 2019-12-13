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
    // Test your grammar online here
    // http://smlweb.cpsc.ucalgary.ca/start.html

    // TODO: Two types of tokens (keywords: '+', '-', 'if' etc) called STRING/LITERAL and ENUM/NAME values that are lexer defined).
    // TODO: How do we connect ENUm/NAME with a STRING/LITERAL/KEYWORD
    // TODOs
    // * Terminals should be an ordered (indexed) set indexed by TTokenKind enum: 0,1,2,...,T (TODO: If EPS is filtered out it should not be zero)
    // * Nonterminals should be an insertion ordered set indexed by the order of declaration in the grammar specification: 0,1.2,...V
    // * Each nonterminal can derive one or more LHS-rules. Each rule is indexed by its order of declaration in the alternative list
    //   of the overall production rule for the terminal:
    //      0. Sentences ---> Name | List 'and' Name
    //      1. Name ---> 'tom' | 'dick' | 'harry'
    //      2. List ---> Name ',' List | Name
    //   This CFG has 3 terminals (Sentence, Name and List). Each production (rule) has one or more LHS alternatives
    //      0. Sentences → Name                 (0,0)
    //                   | List 'and' Name      (0,1)
    //      1. Name      → 'tom'                (1,0)
    //                   | 'dick'               (1,1)
    //                   | 'harry'              (1,2)
    //      2. List      → Name ',' List        (2,0)
    //                   | Name                 (2,1)
    // * That is simple rules can be indexed either by
    //          a pair (i,j)
    //          an index = sumOfRules(0..(i-1)) + j
    // * Terminals.IndexOf('Sentences') == 0
    // * Terminals.IndexOf('Name') == 1
    // * Terminals.IndexOf('List') == 2
    // * ProductionsFor[]
    // NOTE: Both Terminals and Nonterminals can be stored in array, because each type have an Index property.
    // NOTE: Both Terminals and Nonterminals are singleton objects, because grammar symbols carry no state. Only tokens,
    //       CST/AST nodes etc carry state. BUT if the parser should produce interesting output (besides recognizing input)
    //       some values must be defined on a parallel value stack (state connected to the tokens/input).
    //
    // BNF like syntax for describing grammars
    //    ::=   'may produce' / 'derives' / 'is defined as ????'
    //    |     ', or as ???'      (choice)
    //          'followed by ??? (concatenation)
    //    ;     ', and as nothing else'  (punctuation of rule)
    // Extensions
    //   In an extended context-free grammar we can write Something+ meaning “one or more Somethings” and we do not
    //   need to give a rule for Something+. The rule
    //      Something+ → Something | Something Something+
    //   is implicit. The same goes for
    //      Something? → Something | ε
    //      Something* → Something Something* | ε
    //   All the above extensions is done through a “(right-)recursive” interpretation. The method has the advantage
    //   that it is easy to explain and that the transformation to “normal” CF is simple. Disadvantages are that the
    //   transformation entails anonymous rules (identified by α below) and that the parse tree (CST) gets ugly.
    //   The extended rule
    //      Book → Preface Chapter+ Conclusion
    //   gets translated to (new extra anonymous nonterminal α with a right recursive rule)
    //      Book ---> Preface α Conclusion
    //      α ---> Chapter | Chapter α
    //   The extensions of an EBNF grammar do not increase its expressive powers: all implicit rules can be made explicit
    //   and then a normal CF grammar in BNF notation results. Their strength lies in their user-friendliness. The star in
    //   the notation X* with the meaning “a sequence of zero or more Xs” is called the Kleene star.


    /// <summary>
    /// Immutable context-free grammar (CFG) type.
    /// </summary>
    public class Grammar<TTokenKind> : IProductionsContainer, IFollowSymbolsAnalyzer<TTokenKind>
        where TTokenKind : Enum
    {
        private readonly IFollowSymbolsAnalyzer<TTokenKind> _analyzer;

        public Grammar(
            IEnumerable<Nonterminal> variables,
            IEnumerable<Terminal<TTokenKind>> terminals,
            Nonterminal startSymbol,
            IEnumerable<Production> productions,
            Func<Grammar<TTokenKind>, IFollowSymbolsAnalyzer<TTokenKind>> analyzerFactory)
        {
            if (variables == null) throw new ArgumentNullException(nameof(variables));
            if (terminals == null) throw new ArgumentNullException(nameof(terminals));
            if (productions == null) throw new ArgumentNullException(nameof(productions));

            StartSymbol = startSymbol ?? throw new ArgumentNullException(nameof(startSymbol));

            Variables = new InsertionOrderedSet<Nonterminal>(variables);
            Terminals = new Set<Terminal<TTokenKind>>(terminals);

            // Productions are numbered 0,1,2,...,^Productions.Count
            var prods = new List<Production>();
            // Variables (productions on the shorter form (A -> α | β | ...) are numbered 0,1,...,^Variables.Count
            var productionMap = Variables.ToDictionary(symbol => symbol, _ => new List<(int, Production)>());

            int index = 0;
            foreach (var production in productions)
            {
                prods.Add(production);
                productionMap[production.Head].Add((index, production));
                index += 1;
            }

            ProductionsFor = productionMap.ToImmutableDictionary(kvp => kvp.Key,
                kvp => (IReadOnlyList<(int, Production)>) kvp.Value);

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
        public MarkedProduction AugmentedStartItem =>
            new MarkedProduction(Productions[0], 0, 0);

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
        public MarkedProduction AugmentedAcceptItem => Productions[0].LastSymbol.IsEof
            ? new MarkedProduction(Productions[0], 0, Productions[0].Length - 1)
            : new MarkedProduction(Productions[0], 0, Productions[0].Length);

        // NOTE: Our context-free grammars are (always) reduced and augmented!!!!
        // TODO: No useless symbols (required to construct DFA of LR(0) automaton, Knuths Theorem)
        public bool IsReduced => true;

        /// <summary>
        /// The set of nonterminal symbols (aka variables) used to define the grammar. The variables
        /// are defined in the order defined by the sequence of variables passed to the
        /// <see cref="Grammar{TTokenKind}"/> constructor.
        /// </summary>
        public IReadOnlyOrderedSet<Nonterminal> Variables { get; }

        public IEnumerable<Symbol> NonTerminalSymbols => Variables;

        /// <summary>
        /// The set of input symbols used to define the grammar.
        /// </summary>
        /// <remarks>
        /// If the grammar is augmented with an eof marker symbol, the <see cref="Symbol.Eof{TTokenKind}"/> is
        /// included in the <see cref="Terminals"/> set.
        /// </remarks>
        public IReadOnlySet<Terminal<TTokenKind>> Terminals { get; }

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
        public IReadOnlyList<Production> Productions { get; }

        /// <summary>
        /// List of production rules for any given variable (nonterminal symbol).
        /// When A → α | β | ... | ω, then ProductionsFor[A] = α, β,..., ω.
        /// </summary>
        public IReadOnlyDictionary<Nonterminal, IReadOnlyList<(int, Production)>> ProductionsFor { get; }

        public Nonterminal StartSymbol { get; }

        public bool Erasable(int productionIndex)
        {
            return this.Erasable(Productions[productionIndex].Tail);
        }

        public IReadOnlySet<Terminal<TTokenKind>> First(int productionIndex)
        {
            return this.First(Productions[productionIndex].Tail);
        }

        /// <inheritdoc />
        public bool Erasable(Symbol symbol) => _analyzer.Erasable(symbol);

        /// <inheritdoc />
        public IReadOnlySet<Terminal<TTokenKind>> First(Symbol symbol) => _analyzer.First(symbol);

        /// <inheritdoc />
        public IReadOnlySet<Terminal<TTokenKind>> Follow(Nonterminal variable) => _analyzer.Follow(variable);

        /// <summary>
        /// Get NFA representation of the set of characteristic strings (aka viable prefixes) that are defined by
        /// CG = {αβ ∈ Pow(V) | S′ ∗⇒ αAv ⇒ αβv, αβ ∈ Pow(V), v ∈ Pow(T)}, where V := N U V (all grammar symbols),
        /// and ⇒ is the right-most derivation relation. CG is the set of viable prefixes containing all prefixes (αβ)
        /// of right sentential forms (αβv) that can appear on the stack of a shift/reduce parser,
        /// i.e. prefixes of right sentential forms that do not extend past the end of the right-most handle
        /// (A handle, β, of a right sentential form, αβv, is a production, A → β, and a position within the
        /// right sentential form where the substring β can be found).
        /// </summary>
        public LrItemNfa<TTokenKind> GetLr0AutomatonNfa() =>
            Lr0AutomatonAlgorithm.GetLr0AutomatonNfa(this);

        public LrItemNfa<TTokenKind> GetLr1AutomatonNfa() =>
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
        public LrItemsDfa<TTokenKind> GetLr0AutomatonDfa()
        {
            var (states, transitions) = Lr0AutomatonAlgorithm.ComputeLr0AutomatonData(this);
            return Lr0AutomatonAlgorithm.GetLr0AutomatonDfa(this, states, transitions);
        }

        public LrItemsDfa<TTokenKind> GetLr1AutomatonDfa()
        {
            var (states, transitions) = Lr1AutomatonAlgorithm.ComputeLr1AutomatonData(this);
            return Lr1AutomatonAlgorithm.GetLr1AutomatonDfa(this, states, transitions);
        }

        /// <summary>
        /// Compute LR(0) parsing table.
        /// </summary>
        public LrParser<TTokenKind> ComputeLr0ParsingTable()
        {
            var (states, transitions) = Lr0AutomatonAlgorithm.ComputeLr0AutomatonData(this);

            // LR(0)
            var (actionTableEntries, gotoTableEntries) = ComputeParsingTableData(states, transitions,
                (_,__) => Terminals.UnionEofMarker());

            // NOTE: The ParsingTable representation does not have a dead state (not required), and therefore states
            // are given by {0,1,...,N-1}.
            return new LrParser<TTokenKind>(this, states, Variables, Terminals,
                actionTableEntries, gotoTableEntries);
        }

        /// <summary>
        /// Compute SLR(1) parsing table.
        /// </summary>
        public LrParser<TTokenKind> ComputeSlrParsingTable()
        {
            var (states, transitions) = Lr0AutomatonAlgorithm.ComputeLr0AutomatonData(this);

            // SLR(1)
            var (actionTableEntries, gotoTableEntries) = ComputeParsingTableData(states, transitions,
                (_, productionIndex) => Follow(Productions[productionIndex].Head));

            // NOTE: The ParsingTable representation does not have a dead state (not required), and therefore states
            // are given by {0,1,...,N-1}.
            return new LrParser<TTokenKind>(this, states, Variables, Terminals,
                actionTableEntries, gotoTableEntries);
        }

        /// <summary>
        /// Compute LR(1) parsing table.
        /// </summary>
        public LrParser<TTokenKind> ComputeLr1ParsingTable()
        {
            var (states, transitions) = Lr1AutomatonAlgorithm.ComputeLr1AutomatonData(this);

            // LR(1)
            var (actionTableEntries, gotoTableEntries) = ComputeParsingTableData(states, transitions,
                (stateIndex, productionIndex) => states[stateIndex].ReduceBy(productionIndex).Lookaheads);

            // NOTE: The ParsingTable representation does not have a dead state (not required), and therefore states
            // are given by {0,1,...,N-1}.
            return new LrParser<TTokenKind>(this, states, Variables, Terminals,
                actionTableEntries, gotoTableEntries);
        }

        /// <summary>
        /// Compute LALR(1) parsing table (by 'brute force' algorithm based on merging LR(1) item sets with identical
        /// kernel items in the LR(1) automaton).
        /// </summary>
        public LrParser<TTokenKind> ComputeLalrParsingTable()
        {
            var (states, transitions) = Lr1AutomatonAlgorithm.ComputeLr1AutomatonData(this);

            // Merge states and transitions
            var (mergedStates, mergedTransitions) = ComputeMergedLr1AutomatonData(states, transitions);

            // LALR(1)
            var (actionTableEntries, gotoTableEntries) = ComputeParsingTableData(mergedStates, mergedTransitions,
                (stateIndex, productionIndex) => mergedStates[stateIndex].ReduceBy(productionIndex).Lookaheads);

            // NOTE: The ParsingTable representation does not have a dead state (not required), and therefore states
            // are given by {0,1,...,N-1}.
            return new LrParser<TTokenKind>(this, mergedStates, Variables, Terminals,
                actionTableEntries, gotoTableEntries);
        }

        /// <summary>
        /// Compute LALR(1) parsing table (by efficient digraph algorithm that simulates the valid lookahead sets in the LR(0) automaton).
        /// </summary>
        public LrParser<TTokenKind> ComputeEfficientLalr1ParsingTable()
        {
            var (states, transitions) = Lr0AutomatonAlgorithm.ComputeLr0AutomatonData(this);

            var dfaLr0 = Lr0AutomatonAlgorithm.GetLr0AutomatonDfa(this, states, transitions);

            var analyzer = new Lr0AutomatonDigraphAnalyzer<TTokenKind>(this, dfaLr0, _analyzer);

            // LALR(1)
            var (actionTableEntries, gotoTableEntries) = ComputeParsingTableData(states, transitions,
                (stateIndex, productionIndex) => analyzer.Lookaheads(stateIndex, productionIndex));

            // NOTE: The ParsingTable representation does not have a dead state (not required), and therefore states
            // are given by {0,1,...,N-1}.
            return new LrParser<TTokenKind>(this, states, Variables, Terminals,
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
        private (IEnumerable<LrActionEntry<TTokenKind>>, IEnumerable<LrGotoEntry>) ComputeParsingTableData(
            IReadOnlyOrderedSet<ProductionItemSet<TTokenKind>> states,
            List<Transition<Symbol, ProductionItemSet<TTokenKind>>> transitions,
            Func<int, int, IEnumerable<Terminal<TTokenKind>>> reduceOnTerminalSymbols
            )
        {
            var actionTableEntries = new List<LrActionEntry<TTokenKind>>();
            var gotoTableEntries = new List<LrGotoEntry>();

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
                    var a = (Terminal<TTokenKind>)move.Label;
                    // Action[source, a] = shift target
                    actionTableEntries.Add(new LrActionEntry<TTokenKind>(source, a, LrAction.Shift(target)));
                }
                else
                {
                    // If [A → α•Xβ, L] is in LR(k) item set, where X is a nonterminal symbol, and L is an arbitrary
                    // lookahead set (possibly the empty set corresponding to an LR(0) item set)
                    var X = (Nonterminal)move.Label;
                    // Goto[source, X] = target;
                    gotoTableEntries.Add(new LrGotoEntry(source, X, target));
                }
            }

            int stateIndex = 0; // states variable is an ordered set and therefore we can simulate the index without doing any lookup
            foreach (ProductionItemSet<TTokenKind> itemSet in states)
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
                            actionTableEntries.Add(new LrActionEntry<TTokenKind>(stateIndex, terminal, reduceAction));
                        }
                    }
                }

                // If S' → S• is in LR(0) item set, then set action[s, $] to accept
                if (itemSet.IsAcceptAction)
                {
                    actionTableEntries.Add(new LrActionEntry<TTokenKind>(stateIndex,
                        Symbol.Eof<TTokenKind>(), LrAction.Accept));
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
        private (IReadOnlyOrderedSet<ProductionItemSet<TTokenKind>> mergedStates,
            List<Transition<Symbol, ProductionItemSet<TTokenKind>>> mergedTransitions)
            ComputeMergedLr1AutomatonData(
                IReadOnlyOrderedSet<ProductionItemSet<TTokenKind>> states,
                List<Transition<Symbol, ProductionItemSet<TTokenKind>>> transitions)
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
            var mergedStates = new InsertionOrderedSet<ProductionItemSet<TTokenKind>>();
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
                        Dictionary<MarkedProduction, Set<Terminal<TTokenKind>>> itemsMap =
                            state.Items.ToDictionary(item => item.MarkedProduction, item => new Set<Terminal<TTokenKind>>(item.Lookaheads));

                        for (int j = 1; j < blocks[i].Count; j += 1)
                        {
                            var other = states[blocks[i][j]];
                            itemsMap.MergeLookaheads(other.Items
                                .ToDictionary(item => item.MarkedProduction, item => item.Lookaheads));
                        }

                        mergedStates.Add(new ProductionItemSet<TTokenKind>(
                            itemsMap.Select(kvp =>
                                new ProductionItem<TTokenKind>(kvp.Key, kvp.Value))));
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
                new List<Transition<Symbol, ProductionItemSet<TTokenKind>>>();
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
