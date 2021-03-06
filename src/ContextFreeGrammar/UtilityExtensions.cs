using System;
using System.Collections.Generic;
using System.Linq;
using AutomataLib;

namespace ContextFreeGrammar
{
    public static class UtilityExtensions
    {
        public static Set<T> ToUnionSet<T>(this IEnumerable<IReadOnlySet<T>> sets)
            where T : IEquatable<T>
        {
            return sets.Aggregate(new Set<T>(), (unionSet, set) => unionSet.UnionWith(set));
        }

        public static Set<T> ToSet<T>(this IEnumerable<T> items)
            where T : IEquatable<T>
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            return new Set<T>(items);
        }

        public static InsertionOrderedSet<T> ToOrderedSet<T>(this IEnumerable<T> items)
            where T : IEquatable<T>
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            return new InsertionOrderedSet<T>(items);
        }

        public static void MergeLookaheads<TTokenKind>(
            this Dictionary<MarkedProduction, Set<Terminal<TTokenKind>>> dictionary,
            IEnumerable<KeyValuePair<MarkedProduction, IReadOnlySet<Terminal<TTokenKind>>>> other
            ) where TTokenKind : struct, Enum
        {
            foreach (var kvp in other)
            {
                dictionary[kvp.Key].AddRange(kvp.Value);
            }
        }

        public static IEnumerable<T> AsSingletonEnumerable<T>(this T item)
        {
            yield return item;
        }

        public static IEnumerable<T> ConcatItem<T>(this IEnumerable<T> items, T item)
        {
            return  items.Concat(item.AsSingletonEnumerable());
        }

        public static bool Many<TSource>(this IEnumerable<TSource> source) {
            if (source == null) throw new ArgumentNullException(nameof(source));
            int c = 0;
            using (IEnumerator<TSource> e = source.GetEnumerator()) {
                if (e.MoveNext()) c += 1;
                if (c > 1) return true;
            }
            return false;
        }

        public static bool Many<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate) {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            int c = 0;
            foreach (TSource element in source) {
                if (predicate(element)) c += 1;
                if (c > 1) return true;
            }
            return false;
        }

        //public static bool UnionWith<T>(this HashSet<T> hashSet, IEnumerable<T> items)
        //{
        //    var c = hashSet.Count;
        //    hashSet.UnionWith(items);
        //    return hashSet.Count > c;
        //}

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

        // use the null-conditional operator as a really bad Option<T> C# Monad that is not polymorphic between reference and value types
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

        public static string FormatBoolean(this bool value)
        {
            return value ? "true" : "false";
        }

        public static string ToGotoTableString(this int value)
        {
            return value != 0 ? value.ToString() : string.Empty;
        }
    }
}
