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
            return terminalSymbols.Concat(Symbol.Eof<Terminal>().AsSingletonEnumerable());
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

        // use the null-conditional operator as a reaaly bad Option<T> C# Monad that is not polymorphic between reference and value types
        public static T TryGetNext<T>(this IEnumerator<T> iter) where T : class
        {
            return iter.MoveNext()
                ? iter.Current
                : default;
        }

        public static T? TryGetNextValue<T>(this IEnumerator<T> iter) where T : struct
        {
            return iter.MoveNext()
                ? iter.Current
                : default;
        }

        public static string Center(this string str, int totalWidth)
        {
            return str.PadLeft((totalWidth - str.Length) / 2 + str.Length).PadRight(totalWidth);
        }

        public static string FormatBoolean(this bool value)
        {
            return value ? "true" : "false";
        }
    }
}
