using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AutomataLib;

namespace ContextFreeGrammar
{
    /// <summary>
    /// Context-free grammar (CFG)
    /// </summary>
    public class Grammar : IEnumerable<Production>
    {
        // TODO: Do we need a map?
        // private readonly Dictionary<NonTerminal, List<Production>> _productionMap;

        public Grammar(IEnumerable<NonTerminal> variables, IEnumerable<Terminal> terminals, NonTerminal startSymbol)
        {
            Productions = new List<Production>();
            Variables = new HashSet<NonTerminal>(variables);
            Terminals = new HashSet<Terminal>(terminals);
            StartSymbol = startSymbol;
        }

        /// <summary>
        /// First production is a unit production (S -> E), where the head variable (S) has only this single production,
        /// and the head variable (S) is found no where else in any productions.
        /// </summary>
        public bool IsAugmented =>
            Productions[0].Head.Equals(StartSymbol) &&
            Productions.Skip(1).All(p => !p.Head.Equals(StartSymbol)) &&
            Productions.All(p => !p.Tail.Contains(StartSymbol));

        // TODO: No useless symbols
        public bool IsReduced => true;

        /// <summary>
        /// Non-terminal grammar symbols.
        /// </summary>
        public ISet<NonTerminal> Variables { get; }

        /// <summary>
        /// Terminal grammar symbols.
        /// </summary>
        public ISet<Terminal> Terminals { get; }

        /// <summary>
        /// Productions are numbered by index 0,1,2,...
        /// </summary>
        public List<Production> Productions { get; }

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

            Productions.Add(production);
        }

        // TODO: rename to GetClosureItems
        public IEnumerable<ProductionItem> GetEquivalentItemsOf(Symbol variable)
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
                        foreach (var closureItem in GetEquivalentItemsOf(nonTerminal))
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
}
