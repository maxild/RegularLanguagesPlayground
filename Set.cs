using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;


namespace RegExpToDfa
{
    /// <summary>
    /// A set, with element-based hash codes, built upon <see cref="HashSet{T}"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Set<T> : IEquatable<Set<T>>, ICollection<T> where T : IEquatable<T>
    {
        private readonly HashSet<T> _inner;
        private int? _cachedHash; // Cached hash code is valid if non-null (THIS IS SPECIAL)

        public Set()
        {
            _inner = new HashSet<T>();
        }

        // we cannot use capacity because of this constructor
        //public Set(T x) : this()
        //{
        //    Add(x);
        //}

        public Set(IEnumerable<T> coll) : this()
        {
            foreach (T x in coll)
                Add(x);
        }

        public bool Contains(T x)
        {
            return _inner.Contains(x);
        }

        public void Add(T x)
        {
            if (!Contains(x))
            {
                _inner.Add(x);
                _cachedHash = null;
            }
        }

        public bool Remove(T x)
        {
            bool removed = _inner.Remove(x);
            if (removed)
                _cachedHash = null;
            return removed;
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

        public void CopyTo(T[] arr, int i)
        {
            _inner.CopyTo(arr, i);
        }

        public void Clear()
        {
            _inner.Clear();
            _cachedHash = null;
        }

        public bool IsReadOnly => false;

        // Is this set a subset of that?
        public bool IsSubsetOf(Set<T> that)
        {
            foreach (T x in this)
                if (!that.Contains(x))
                    return false;
            return true;
        }

        // Create new set as intersection of this and that
        public Set<T> Intersection(Set<T> that)
        {
            Set<T> result = new Set<T>();
            foreach (T x in this)
                if (that.Contains(x))
                    result.Add(x);
            return result;
        }

        // Create new set as union of this and that
        public Set<T> Union(Set<T> that)
        {
            Set<T> result = new Set<T>(this);
            foreach (T x in that)
                result.Add(x);
            return result;
        }

        // Create new set as difference between this and that
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

        // Create new set as symmetric difference between this and that
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
                int hashCode = 0;
                foreach (T x in this)
                    hashCode ^= x.GetHashCode();
                _cachedHash = hashCode;
            }

            return _cachedHash.Value;
        }

        public bool Equals(Set<T> other)
        {
            return other != null && other.Count == Count && other.IsSubsetOf(this);
        }

        public override string ToString()
        {
            return this.ToSetNotation();
        }
    }

    public static class SetExtenions
    {
        public static string ToSetNotation<T>(this IEnumerable<T> values)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{ ");
            bool first = true;
            foreach (T x in values)
            {
                if (!first)
                    sb.Append(", ");
                sb.Append(x);
                first = false;
            }
            sb.Append(" }");
            return sb.ToString();
        }
    }
}
