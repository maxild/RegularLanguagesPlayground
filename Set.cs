using System;
using System.Collections.Generic;
using System.Text;


namespace RegExpToDfa
{
    // TODO: Is this needed today???

    /// <summary>
    /// A set, with element-based hash codes, built upon <see cref="HashSet{T}"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Set<T> : IEquatable<Set<T>>, ICollection<T> where T : IEquatable<T>
    {
        private readonly HashSet<T> _inner = new HashSet<T>();
        private int? _cachedHash; // Cached hash code is valid if non-null (THIS IS SPECIAL)

        public Set()
        {
        }

        public Set(T x) : this()
        {
            Add(x);
        }

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
            Set<T> res = new Set<T>();
            foreach (T x in this)
                if (that.Contains(x))
                    res.Add(x);
            return res;
        }

        // Create new set as union of this and that
        public Set<T> Union(Set<T> that)
        {
            Set<T> res = new Set<T>(this);
            foreach (T x in that)
                res.Add(x);
            return res;
        }

        // Create new set as difference between this and that
        public Set<T> Difference(Set<T> that)
        {
            Set<T> res = new Set<T>();
            foreach (T x in this)
                if (!that.Contains(x))
                    res.Add(x);
            return res;
        }

        // Create new set as symmetric difference between this and that
        public Set<T> SymmetricDifference(Set<T> that)
        {
            Set<T> res = new Set<T>();
            foreach (T x in this)
                if (!that.Contains(x))
                    res.Add(x);
            foreach (T x in that)
                if (!Contains(x))
                    res.Add(x);
            return res;
        }

        // Compute hash code based on set contents, and cache it
        public override int GetHashCode()
        {
            if (!_cachedHash.HasValue)
            {
                int res = 0;
                foreach (T x in this)
                    res ^= x.GetHashCode();
                _cachedHash = res;
            }

            return _cachedHash.Value;
        }

        public bool Equals(Set<T> other)
        {
            return other != null && other.Count == Count && other.IsSubsetOf(this);
        }

        public override string ToString()
        {
            StringBuilder res = new StringBuilder();
            res.Append("{ ");
            bool first = true;
            foreach (T x in this)
            {
                if (!first)
                    res.Append(", ");
                res.Append(x);
                first = false;
            }

            res.Append(" }");
            return res.ToString();
        }
    }
}