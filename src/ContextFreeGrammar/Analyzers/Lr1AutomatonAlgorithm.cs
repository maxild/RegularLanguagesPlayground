using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using AutomataLib;

namespace ContextFreeGrammar.Analyzers
{
    public static class Lr1AutomatonAlgorithm
    {
        // TODO: Make it DRY between LR(0) and LR(1)...compare with method above
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public static Nfa<ProductionItem<TNonterminalSymbol, TTerminalSymbol>, Symbol> GetLr1AutomatonNfa<TNonterminalSymbol, TTerminalSymbol>(
            Grammar<TNonterminalSymbol, TTerminalSymbol> grammar)
                where TNonterminalSymbol : Symbol, IEquatable<TNonterminalSymbol>
                where TTerminalSymbol : Symbol, IEquatable<TTerminalSymbol>
        {
            if (!grammar.IsAugmented)
            {
                throw new InvalidOperationException("The grammar should be augmented with canonical S' → S production.");
            }

            if (!grammar.IsReduced)
            {
                throw new InvalidOperationException("The grammar contains useless symbols.");
            }

            // Lookahead of items are defined by
            //

            // The start state is [S' → •S, $]
            var startItem = new ProductionItem<TNonterminalSymbol, TTerminalSymbol>(grammar.Productions[0], 0, 0, Symbol.Eof<TTerminalSymbol>());

            var transitions = new List<Transition<Symbol, ProductionItem<TNonterminalSymbol, TTerminalSymbol>>>();
            var acceptItems = new List<ProductionItem<TNonterminalSymbol, TTerminalSymbol>>();

            var states = new HashSet<ProductionItem<TNonterminalSymbol, TTerminalSymbol>>(startItem.AsSingletonEnumerable());

            var worklist = new Queue<ProductionItem<TNonterminalSymbol, TTerminalSymbol>>();
            worklist.Enqueue(startItem);

            // (a) For every terminal a ∈ T, if [A → α•aβ, b] is a marked production, then
            //     there is a transition on input a from state [A → α•aβ, b] to state [A → αa•β, b]
            //     obtained by "shifting the dot" (where a = b is possible)
            // (b) For every variable B ∈ N, if [A → α•Bβ, b] is a marked production, then
            //     there is a transition on input B from state [A → α•Bβ, b] to state [A → αB•β, b]
            //     obtained by "shifting the dot", and transitions on input ε (the empty string)
            //     to all states [B → •γ, a], for all productions B → γ ∈ P with left-hand side B
            //     and a ∈ FIRST(βb).
            while (worklist.Count > 0)
            {
                var item = worklist.Dequeue();

                // (a) [A → α•aβ, b] --a--> [A → αa•β, b]
                if (item.IsShiftItem)
                {
                    Symbol a = item.GetDotSymbol<Terminal>();
                    var shiftToItem = item.WithShiftedDot();
                    if (states.Add(shiftToItem))
                    {
                        worklist.Enqueue(shiftToItem);
                    }
                    transitions.Add(Transition.Move(item, a, shiftToItem));
                }

                // (b) [A → α•Bβ, b] (with new CLOSURE function, because of lookahead)
                if (item.IsGotoItem)
                {
                    var B = item.GetDotSymbol<TNonterminalSymbol>();
                    var gotoItem = item.WithShiftedDot();
                    if (states.Add(gotoItem))
                    {
                        worklist.Enqueue(gotoItem);
                    }
                    transitions.Add(Transition.Move(item, (Symbol)B, gotoItem));

                    // closure items (with changed lookahead symbols) represented by ε-transitions
                    foreach (var (index, productionOfB) in grammar.ProductionsFor[B])
                    {
                        // Expecting to see 'Bβ', where B ∈ T, followed by lookahead symbol 'b' of [A → α•Bβ, b]
                        // is the same as expecting to see any grammar symbols 'γ' followed by lookahead
                        // symbol 'a' of [B → γ, a], where a ∈ FIRST(βb) and 'B → γ' is a production ∈ P.
                        Symbol b = item.Lookaheads.Single();
                        foreach (TTerminalSymbol a in grammar.First(item.GetRemainingSymbolsAfterDotSymbol().ConcatItem(b)))
                        {
                            // [B → γ, a]
                            var closureItem = new ProductionItem<TNonterminalSymbol, TTerminalSymbol>(productionOfB, index, 0, a);
                            if (states.Add(closureItem))
                            {
                                worklist.Enqueue(closureItem);
                            }
                            transitions.Add(
                                Transition.EpsilonMove<Symbol, ProductionItem<TNonterminalSymbol, TTerminalSymbol>>(item, closureItem));
                        }
                    }
                }

                // (c) [A → β•, b] completed item with dot in rightmost position
                if (item.IsReduceItem)
                {
                    acceptItems.Add(item);
                }
            }

            return new Nfa<ProductionItem<TNonterminalSymbol, TTerminalSymbol>, Symbol>(transitions, startItem, acceptItems);
        }

        public static Dfa<ProductionItemSet<TNonterminalSymbol, TTerminalSymbol>, Symbol> GetLr1AutomatonDfa<TNonterminalSymbol, TTerminalSymbol>(
            Grammar<TNonterminalSymbol, TTerminalSymbol> grammar)
            where TNonterminalSymbol : Symbol, IEquatable<TNonterminalSymbol>
            where TTerminalSymbol : Symbol, IEquatable<TTerminalSymbol>
        {
            var (states, transitions) = ComputeLr1AutomatonData(grammar);

            var acceptStates = states.Where(itemSet => itemSet.ReduceItems.Any()).ToList();

            // NOTE: This DFA representation always need to have a so called dead state (0),
            // and {1,2,...,N} are therefore the integer values of the actual states.
            return new Dfa<ProductionItemSet<TNonterminalSymbol, TTerminalSymbol>, Symbol>(states, grammar.Symbols,
                transitions, states[0], acceptStates);
        }

        // NOTE: LR(1) items have merged lookahead sets in order for LR(1) item sets (i.e. states) to have the minimal number of LR(1) items
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public static (
            IReadOnlyOrderedSet<ProductionItemSet<TNonterminalSymbol, TTerminalSymbol>> states,
            List<Transition<Symbol, ProductionItemSet<TNonterminalSymbol, TTerminalSymbol>>> transitions
            )
            ComputeLr1AutomatonData<TNonterminalSymbol, TTerminalSymbol>(Grammar<TNonterminalSymbol, TTerminalSymbol> grammar)
            where TNonterminalSymbol : Symbol, IEquatable<TNonterminalSymbol>
            where TTerminalSymbol : Symbol, IEquatable<TTerminalSymbol>
        {
            ProductionItemSet<TNonterminalSymbol, TTerminalSymbol> startItemSet =
                Closure(grammar, new ProductionItem<TNonterminalSymbol, TTerminalSymbol>(grammar.Productions[0], 0, 0,
                    Symbol.Eof<TTerminalSymbol>()).AsSingletonEnumerable());

            // states (aka LR(k) items) er numbered 0,1,2...in insertion order, such that the start state is always at index zero.
            var states = new InsertionOrderedSet<ProductionItemSet<TNonterminalSymbol, TTerminalSymbol>>(startItemSet.AsSingletonEnumerable());
            var transitions = new List<Transition<Symbol, ProductionItemSet<TNonterminalSymbol, TTerminalSymbol>>>();

            var worklist = new Queue<ProductionItemSet<TNonterminalSymbol, TTerminalSymbol>>(startItemSet.AsSingletonEnumerable());
            while (worklist.Count > 0)
            {
                ProductionItemSet<TNonterminalSymbol, TTerminalSymbol> sourceState = worklist.Dequeue();
                // For each successor item pair (X, { [A → αX•β, b], where the item [A → α•Xβ, b] is in the predecessor item set}),
                // where [A → αX•β, b] is a kernel successor item on some grammar symbol X in V, where V := N U T
                foreach (var kernelSuccessorItems in sourceState.GetTargetItems())
                {
                    // For each transition grammar symbol (label on the transition/edge in the graph)
                    var X = kernelSuccessorItems.Key; // can be either terminal (goto) or nonterminal (shift/read)
                    // Get the closure of all the kernel successor items A → αX•β that we can move/transition to in the graph
                    ProductionItemSet<TNonterminalSymbol, TTerminalSymbol> targetState = Closure(grammar, kernelSuccessorItems);
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
        /// Compute ε-closure of the kernel items of any LR(1) item set --- this
        /// is identical to ε-closure in the subset construction algorithm when translating
        /// NFA to DFA.
        /// </summary>
        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static ProductionItemSet<TNonterminalSymbol, TTerminalSymbol> Closure<TNonterminalSymbol, TTerminalSymbol>(
            Grammar<TNonterminalSymbol, TTerminalSymbol> grammar,
            IEnumerable<ProductionItem<TNonterminalSymbol, TTerminalSymbol>> kernelItems)
            where TNonterminalSymbol : Symbol, IEquatable<TNonterminalSymbol>
            where TTerminalSymbol : Symbol, IEquatable<TTerminalSymbol>
        {
            var closure = new HashSet<ProductionItem<TNonterminalSymbol, TTerminalSymbol>>(kernelItems);

            var worklist = new Queue<ProductionItem<TNonterminalSymbol, TTerminalSymbol>>(kernelItems);
            while (worklist.Count != 0)
            {
                ProductionItem<TNonterminalSymbol, TTerminalSymbol> item = worklist.Dequeue();
                // B is the next symbol (that must be a nonterminal symbol)
                var B = item.TryGetDotSymbol<TNonterminalSymbol>();
                if (B == null) continue;
                // If item is a GOTO item of the form [A → α•Bβ, b], where B ∈ T,
                // then find all its closure items [B → γ, a], where a ∈ FIRST(βb)
                // and 'B → γ' is a production ∈ P.
                //
                //      or
                //
                // Expecting to see 'Bβ', where B ∈ T, followed by lookahead symbol 'b' of [A → α•Bβ, b]
                // is the same as expecting to see any grammar symbols 'γ' followed by lookahead
                // symbol 'a' of [B → •γ, a], where a ∈ FIRST(βb) and 'B → γ' is a production ∈ P.
                var beta = item.GetRemainingSymbolsAfterDotSymbol();

                // Because 'merged' items can have lookahead sets with many terminal symbols we have to
                // calculate the union of all FIRST(βb) for every b ∈ L, where L is the lookahead
                // set of item [A → α•Bβ, L]
                var lookaheads = item.Lookaheads.Aggregate(new Set<TTerminalSymbol>(), (l, b) => l.UnionWith(grammar.First(beta.ConcatItem(b))));
                //var lookaheads = item.Lookaheads.Select(b => FIRST(beta.ConcatItem(b))).ToUnionSet();

                foreach (var (index, production) in grammar.ProductionsFor[B])
                {
                    foreach (TTerminalSymbol a in lookaheads)
                    {
                        // [B → •γ, a]
                        var closureItem = new ProductionItem<TNonterminalSymbol, TTerminalSymbol>(production, index, 0, a);
                        if (!closure.Contains(closureItem))
                        {
                            closure.Add(closureItem);
                            worklist.Enqueue(closureItem);
                        }
                    }
                }
            }

            // This merge operation will make every marked production of an LR(1) item unique within each LR(1) item set.
            // This will simplify the merging of different LR(1) item sets into merged LALR(1) item sets, and it will also make
            // every LR(1) item set a bit more lightweight, because LR(1) items (with identical marked productions) can be
            // represented by a single LR(1) item with lookahead symbols defined by the union of the merged items.
            var closureWithMergedItems =
                from lookaheadsOfMarkedProduction in closure.ToLookup(x => x.MarkedProduction, x => x.Lookaheads)
                let firstItem = new ProductionItem<TNonterminalSymbol, TTerminalSymbol>(
                    markedProduction: lookaheadsOfMarkedProduction.Key,
                    lookaheads: lookaheadsOfMarkedProduction.First())
                select lookaheadsOfMarkedProduction.Skip(1).Aggregate(firstItem,
                    (nextItem, lookaheads) => nextItem.WithUnionLookaheads(lookaheads));

            return new ProductionItemSet<TNonterminalSymbol, TTerminalSymbol>(closureWithMergedItems);
        }
    }
}
