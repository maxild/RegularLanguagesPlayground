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
