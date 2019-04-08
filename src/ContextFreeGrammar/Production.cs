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

    // TODO: Remove this
    public interface INumberedItem
    {
        string Id { get; }
        string Label { get; }
    }


    // TODO: Remove this
    public static class NumberUtils
    {
        public static int CombineDWords(ushort low, ushort high)
        {
            return low & 0xffff | ((high & 0xffff) << 16);
        }

        public static ushort HighDWord(uint a)
        {
            return (ushort)(a >> 16);
        }

        public static ushort HighDWord(int a)
        {
            return (ushort)(a >> 16);
        }

        public static ushort LowDWord(uint a)
        {
            return (ushort)(a & 0xffff);
        }

        public static ushort LowDWord(int a)
        {
            return (ushort)(a & 0xffff);
        }
    }
}
