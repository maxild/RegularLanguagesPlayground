using System;
using System.Collections.Generic;
using System.Linq;
using AutomataLib;

namespace ContextFreeGrammar
{
    public static class UtilityExtensions
    {
        public static IEnumerable<T> AsSingletonEnumerable<T>(this T item)
        {
            yield return item;
        }

        //public static bool AddRange<T>(this HashSet<T> hashSet, IEnumerable<T> items)
        //{
        //    var c = hashSet.Count;
        //    hashSet.UnionWith(items);
        //    return hashSet.Count > c;
        //}

        public static IEnumerable<Terminal> WithEofMarker(this IEnumerable<Terminal> terminalSymbols)
        {
            return terminalSymbols.Concat(Symbol.Eof.AsSingletonEnumerable());
        }

        public static void Each<T>(this IEnumerable<T> e, Action<T> a)
        {
            foreach (var i in e) a(i);
        }

        public static IEnumerable<T> PopItemsOfLengthResult<T>(this Stack<T> stack, int length)
        {
            while (length-- > 0)
                yield return stack.Pop();
        }

        public static void PopItemsOfLength<T>(this Stack<T> stack, int length)
        {
            while (length-- > 0)
                stack.Pop();
        }
    }
}
