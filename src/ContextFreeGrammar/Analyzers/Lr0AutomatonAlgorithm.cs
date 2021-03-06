using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using AutomataLib;

namespace ContextFreeGrammar.Analyzers
{
    public static class Lr0AutomatonAlgorithm
    {
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
        public static LrItemNfa<TTokenKind> GetLr0AutomatonNfa<TTokenKind, TNonterminal>(Grammar<TTokenKind, TNonterminal> grammar)
            where TTokenKind : struct, Enum
            where TNonterminal : struct, Enum
        {
            // NOTE: These are all synonyms for what machine we are building here
            //          - 'characteristic strings' recognizer
            //          - 'viable prefix' recognizer  (αβ is the viable prefix on top of the the stack)
            //          - 'handle' recognizer         (β is the handle on top of the stack)
            //          - LR(0) automaton

            if (!grammar.IsAugmented)
            {
                throw new InvalidOperationException("The grammar should be augmented with canonical S' → S production.");
            }

            if (!grammar.IsReduced)
            {
                throw new InvalidOperationException("The grammar contains useless symbols.");
            }

            var startItem = new ProductionItem<TTokenKind>(grammar.Productions[0], 0, 0);
            var transitions = new List<Transition<Symbol, ProductionItem<TTokenKind>>>();
            var acceptItems = new List<ProductionItem<TTokenKind>>();

            // (a) For every terminal a ∈ T, if A → α•aβ is a marked production, then
            //     there is a transition on input a from state A → α•aβ to state A → αa•β
            //     obtained by "shifting the dot"
            // (b) For every variable B ∈ V, if A → α•Bβ is a marked production, then
            //     there is a transition on input B from state A → α•Bβ to state A → αB•β
            //     obtained by "shifting the dot", and transitions on input ε (the empty string)
            //     to all states B → •γ(i), for all productions B → γ(i) ∈ P with left-hand side B.
            int productionIndex = 0;
            foreach (var production in grammar.Productions)
            {
                for (int dotPosition = 0; dotPosition <= production.Tail.Count; dotPosition += 1)
                {
                    // (productionIndex, dotPosition) is identifier
                    var item = new ProductionItem<TTokenKind>(production, productionIndex, dotPosition);

                    // (a) A → α•aβ
                    if (item.IsShiftItem)
                    {
                        Symbol a = item.GetDotSymbol<Terminal<TTokenKind>>();
                        var shiftToItem = item.WithShiftedDot();
                        transitions.Add(Transition.Move(item, a, shiftToItem));
                    }

                    // (b) A → α•Bβ
                    if (item.IsGotoItem)
                    {
                        var B = item.GetDotSymbol<Nonterminal>();
                        var gotoItem = item.WithShiftedDot();
                        transitions.Add(Transition.Move(item, (Symbol)B, gotoItem));

                        // closure items
                        foreach (var (index, productionOfB) in grammar.ProductionsFor[B])
                        {
                            var closureItem = new ProductionItem<TTokenKind>(productionOfB, index, 0);
                            // Expecting to see a nonterminal 'B' (of Bβ) is the same as expecting to see
                            // RHS grammar symbols 'γ(i)', where B → γ(i) is a production ∈ P
                            transitions.Add(Transition.Move(item, Symbol.Epsilon, closureItem));
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

            return new LrItemNfa<TTokenKind>(transitions, startItem, acceptItems);
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
        public static LrItemsDfa<TTokenKind> GetLr0AutomatonDfa<TTokenKind, TNonterminal>(
            Grammar<TTokenKind, TNonterminal> grammar,
            IReadOnlyOrderedSet<ProductionItemSet<TTokenKind>> states,
            IEnumerable<Transition<Symbol, ProductionItemSet<TTokenKind>>> transitions
            )
            where TTokenKind : struct, Enum
            where TNonterminal : struct, Enum
        {
            var acceptStates = states.Where(itemSet => itemSet.ReduceItems.Any()).ToList();

            // NOTE: This DFA representation always need to have a so called dead state (0),
            // and {1,2,...,N} are therefore the integer values of the actual states.
            return new LrItemsDfa<TTokenKind>(states, grammar.Symbols,
                transitions, states[0], acceptStates);
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
        public static (
            IReadOnlyOrderedSet<ProductionItemSet<TTokenKind>> states,
            List<Transition<Symbol, ProductionItemSet<TTokenKind>>> transitions
            )
            ComputeLr0AutomatonData<TTokenKind, TNonterminal>(Grammar<TTokenKind, TNonterminal> grammar)
            where TTokenKind : struct, Enum
            where TNonterminal: struct, Enum
        {
            ProductionItemSet<TTokenKind> startItemSet =
                Closure(grammar, new ProductionItem<TTokenKind>(grammar.Productions[0], 0, 0).AsSingletonEnumerable());

            // states (aka LR(0) items) er numbered 0,1,2...in insertion order, such that the start state is always at index zero.
            // when adding states to the DFA we transform the states to 1,2,...,N (because the dead state is state 0)
            var states = new InsertionOrderedSet<ProductionItemSet<TTokenKind>>(startItemSet.AsSingletonEnumerable());
            var transitions = new List<Transition<Symbol, ProductionItemSet<TTokenKind>>>();

            var worklist = new Queue<ProductionItemSet<TTokenKind>>(startItemSet.AsSingletonEnumerable());
            while (worklist.Count > 0)
            {
                ProductionItemSet<TTokenKind> sourceState = worklist.Dequeue();
                // For each pair (X, { A → αX•β, where the item A → α•Xβ is in the predecessor item set}),
                // where A → αX•β is kernel successor item on some grammar symbol X in the graph
                foreach (var kernelSuccessorItems in sourceState.GetTargetItems())
                {
                    // For each grammar symbol (label on the transition/edge in the graph)
                    var X = kernelSuccessorItems.Key;
                    // Get the closure of all the kernel successor items A → αX•β that we can move/transition to in the graph
                    ProductionItemSet<TTokenKind> targetState = Closure(grammar, kernelSuccessorItems);
                    transitions.Add(Transition.Move(sourceState, X, targetState));
                    if (!states.Contains(targetState))
                    {
                        worklist.Enqueue(targetState);
                        states.Add(targetState);
                    }
                }
            }

            return (states, transitions);
        }

        /// <summary>
        /// Compute ε-closure of the kernel items of any LR(0) item set --- this
        /// is identical to ε-closure in the subset construction algorithm when translating
        /// NFA to DFA.
        /// </summary>
        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static ProductionItemSet<TTokenKind> Closure<TTokenKind, TNonterminal>(
            Grammar<TTokenKind, TNonterminal> grammar,
            IEnumerable<ProductionItem<TTokenKind>> kernelItems
            )
            where TTokenKind : struct, Enum
            where TNonterminal: struct, Enum
        {
            var closure = new HashSet<ProductionItem<TTokenKind>>(kernelItems);

            var worklist = new Queue<ProductionItem<TTokenKind>>(kernelItems);
            while (worklist.Count != 0)
            {
                ProductionItem<TTokenKind> item = worklist.Dequeue();
                var B = item.TryGetDotSymbol<Nonterminal>();
                if (B == null) continue;
                // If item is a GOTO item of the form A → α•Bβ, where B ∈ T,
                // then find all its closure items
                foreach (var (index, production) in grammar.ProductionsFor[B])
                {
                    var closureItem = new ProductionItem<TTokenKind>(production, index, 0);
                    if (!closure.Contains(closureItem))
                    {
                        closure.Add(closureItem);
                        worklist.Enqueue(closureItem);
                    }
                }
            }

            return new ProductionItemSet<TTokenKind>(closure);
        }
    }
}
