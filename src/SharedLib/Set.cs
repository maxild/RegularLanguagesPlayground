using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace AutomataLib
{
    /// <summary>
    /// A set, with element-based hash codes, built upon <see cref="HashSet{T}"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [DebuggerTypeProxy(typeof(EnumerableDebugView<>)), DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
    public class Set<T> : ISet<T>, IEquatable<Set<T>> where T : IEquatable<T>
    {
        private string DebuggerDisplay => Count > 0
            ? $"{this.ToVectorString()}, Count = {Count}"
            : $"Count = {Count}";

        private readonly HashSet<T> _inner;
        private int? _cachedHash; // Cached hash code is valid if non-null (THIS IS SPECIAL)

        public Set()
        {
            _inner = new HashSet<T>();
        }

        public Set(IEnumerable<T> items) : this()
        {
            _inner = new HashSet<T>(items);
        }

        public bool Contains(T item)
        {
            return _inner.Contains(item);
        }

        public bool Add(T item)
        {
            bool added = _inner.Add(item);
            if (added)
                _cachedHash = null;
            return added;
        }

        public bool AddRange(IEnumerable<T> other)
        {
            int c = _inner.Count;
            _inner.UnionWith(other);
            bool added = _inner.Count > c;
            if (added)
                _cachedHash = null;
            return added;
        }

        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            return _inner.IsProperSubsetOf(other);
        }

        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            return _inner.IsProperSupersetOf(other);
        }

        public bool IsSubsetOf(IEnumerable<T> other)
        {
            return _inner.IsSubsetOf(other);
        }

        public bool IsSupersetOf(IEnumerable<T> other)
        {
            return _inner.IsSupersetOf(other);
        }

        public bool Overlaps(IEnumerable<T> other)
        {
            return _inner.Overlaps(other);
        }

        public bool SetEquals(IEnumerable<T> other)
        {
            return _inner.SetEquals(other);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _inner.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count => _inner.Count;

        public void CopyTo(T[] array, int arrayIndex)
        {
            _inner.CopyTo(array, arrayIndex);
        }

        public Set<T> Intersection(Set<T> that)
        {
            Set<T> result = new Set<T>();
            foreach (T x in this)
                if (that.Contains(x))
                    result.Add(x);
            return result;
        }

        public Set<T> Union(Set<T> that)
        {
            Set<T> result = new Set<T>(this);
            foreach (T x in that)
                result.Add(x);
            return result;
        }

        public Set<T> Difference(Set<T> that)
        {
            Set<T> result = new Set<T>();
            foreach (T x in this)
                if (!that.Contains(x))
                    result.Add(x);
            return result;
        }

        public Set<T> Difference(IEnumerable<Set<T>> that)
        {
            Set<T> result = new Set<T>();
            IEnumerable<Set<T>> enumerable = that as Set<T>[] ?? that as IList<Set<T>> ?? that.ToArray();
            foreach (T x in this)
            {
                if (!enumerable.Any(other => other.Contains(x)))
                    result.Add(x);
            }
            return result;
        }

        public Set<T> SymmetricDifference(Set<T> that)
        {
            Set<T> result = new Set<T>();
            foreach (T x in this)
                if (!that.Contains(x))
                    result.Add(x);
            foreach (T x in that)
                if (!Contains(x))
                    result.Add(x);
            return result;
        }

        // Compute hash code based on set contents, and cache it
        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode()
        {
            // NOTE: we reset the cached hash code to null if Set is mutated
            if (!_cachedHash.HasValue)
            {
                // we use prime numbers
                int hashCode = 17;
                foreach (T x in this)
                    hashCode = 31 * hashCode + x.GetHashCode();
                _cachedHash = hashCode;
            }

            return _cachedHash.Value;
        }

        public bool Equals(Set<T> other)
        {
            return other != null && _inner.SetEquals(other);
        }

        public override string ToString()
        {
            return this.ToVectorString();
        }
    }
}