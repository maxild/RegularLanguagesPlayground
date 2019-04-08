using System.Collections.Generic;
using System.Linq;
using AutomataLib;

namespace ContextFreeGrammar
{
    public class Production
    {
        public Production(NonTerminal head, IEnumerable<Symbol> tail)
        {
            Head = head;
            Tail = tail.ToList();
        }

        /// <summary>
        /// LHS
        /// </summary>
        public NonTerminal Head { get; }

        /// <summary>
        /// RHS
        /// </summary>
        public List<Symbol> Tail { get; }

        public override string ToString()
        {
            return $"{Head} → {string.Join(string.Empty, Tail.Select(symbol => symbol.Name))}";
        }
    }
}
