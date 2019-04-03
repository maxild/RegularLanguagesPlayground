using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ContextFreeGrammar
{
    /// <summary>
    /// Context-free grammar (CFG)
    /// </summary>
    public class Grammar : IEnumerable<Production>
    {
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

        //public IEnumerable<ProductionItem> DottedItems
        //{
        //    get
        //    {
        //        int productionIndex = 0;
        //        foreach (var production in Productions)
        //        {
        //            foreach (var symbol in production.Tail)
        //            {
        //                yield return new ProductionItem(production, productionIndex, );
        //            }

        //            productionIndex += 0;
        //        }
        //    }
        //}



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


    /// <summary>
    /// A transition to another state on a given label in the transition graph.
    /// </summary>
    public struct Transition<TState, TAlphabet>
    {
        /// <summary>
        /// Input (character) that labels the transition
        /// </summary>
        public TAlphabet Label;

        /// <summary>
        /// State we transition into on <see cref="Label"/>
        /// </summary>
        public TState ToState;

        public Transition(TAlphabet label, TState toState)
        {
            Label = label;
            ToState = toState;
        }

        public override string ToString()
        {
            return "-" + Label + "-> " + ToState;
        }
    }
}
