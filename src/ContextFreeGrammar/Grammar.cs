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

        public IEnumerable<ProductionItem> GetEquivalentItemsOf(NonTerminal variable)
        {
            for (int i = 0; i < Productions.Count; i++)
            {
                if (Productions[i].Head.Equals(variable))
                {
                    yield return new ProductionItem(Productions[i], i, 0);
                }
            }
        }

        // Step 1 of 2: The canonical collection of sets of LR(0) items
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

            // Create NFA (digraph of items labeled by symbols)
            var characteristicStringsNfa = new Nfa<ProductionItem, Symbol>(new ProductionItem(Productions[0], 0, 0));

            // (a) For every terminal a in T, if A → α.aβ is a marked production, then
            //     there is a transition on input a from state A → α.aβ to state A → αa.β
            //     obtained by "shifting the dot"
            // (b) For every variable B in V, if A → α.Bβ is a marked production, then
            //     there is a transition on input B from state A → α.Bβ to state A → αB.β
            //     obtained by "shifting the dot", and transitions on input ϵ (the empty string)
            //     to all states B → .γ(i), for all productions B → γ(i) in P with left-hand side B.
            int productionIndex = 0;
            foreach (var production in Productions)
            {
                for (int dotPosition = 0; dotPosition <= production.Tail.Count; dotPosition += 1)
                {
                    // (productionIndex, dotPosition) is identifier
                    var item = new ProductionItem(production, productionIndex, dotPosition);

                    // (a) A → α.aβ
                    if (item.IsShiftItem)
                    {
                        // shift item
                        characteristicStringsNfa.AddTransition(item, item.GetNextSymbol<Terminal>(), item.GetNextItem());
                    }

                    // (b) A → α.Bβ
                    if (item.IsGotoItem)
                    {
                        var nonTerminal = item.GetNextSymbol<NonTerminal>();
                        // goto item
                        characteristicStringsNfa.AddTransition(item, nonTerminal, item.GetNextItem());
                        // closure items
                        foreach (var closureItems in GetEquivalentItemsOf(nonTerminal))
                        {
                            // Expecting to see a non terminal 'B' is the same as expecting to see
                            // RHS grammar symbols 'γ(i)', where B → γ(i) is a production in P
                            characteristicStringsNfa.AddTransition(item, Symbol.Epsilon, closureItems);
                        }
                    }

                    // (c) A → β. Accepting states has dot shifted all the way to the end.
                    if (item.IsReduceItem)
                    {
                        characteristicStringsNfa.AcceptingStates.Add(item);
                    }
                }

                productionIndex += 1;
            }

            return characteristicStringsNfa;
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
