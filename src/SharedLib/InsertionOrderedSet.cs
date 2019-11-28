using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace AutomataLib
{
    /// <summary>
    /// A set, that preserves insertion order.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
    [DebuggerTypeProxy(typeof(EnumerableDebugView<>)), DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
    public class InsertionOrderedSet<T> : ISet<T>, IReadOnlyOrderedSet<T>, IEquatable<InsertionOrderedSet<T>>
        where T : IEquatable<T>
    {
        private string DebuggerDisplay => Count > 0
            ? $"{this.ToVectorString()}, Count = {Count}"
            : $"Count = {Count}";

        private readonly IDictionary<T, int> _dictionary;
        private readonly List<T> _list;
        private int? _cachedHash; // Cached hash code is valid if non-null (THIS IS SPECIAL)

        public InsertionOrderedSet()
            : this(EqualityComparer<T>.Default)
        {
        }

        public InsertionOrderedSet(IEnumerable<T> items)
            : this(EqualityComparer<T>.Default)
        {
            AddRange(items);
        }

        private InsertionOrderedSet(IEqualityComparer<T> comparer)
        {
            _dictionary = new Dictionary<T, int>(comparer);
            _list = new List<T>();
        }

        public int Count => _list.Count;

        public T this[int index] => _list[index];

        public int IndexOf(T item)
        {
            return _dictionary.TryGetValue(item, out var index) ? index : -1;
        }

        public bool Add(T item)
        {
            if (_dictionary.ContainsKey(item)) return false;
            _list.Add(item);
            _dictionary.Add(item, _list.Count - 1);
            _cachedHash = null;
            return true;
        }

        public int TryAdd(T item)
        {
            if (_dictionary.TryGetValue(item, out var index))
                return index;
            Add(item);
            return _list.Count - 1;
        }

        public bool AddRange(IEnumerable<T> other)
        {
            int c = _list.Count;
            foreach (var item in other)
                Add(item);
            bool added = c != _list.Count;
            if (added)
                _cachedHash = null;
            return added;
        }

        public InsertionOrderedSet<T> UnionWith(IEnumerable<T> other)
        {
            AddRange(other);
            return this;
        }

        public bool Contains(T item)
        {
            return _dictionary.ContainsKey(item);
        }

        public bool IsSubsetOf(IEnumerable<T> other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));
            var otherHashset = new HashSet<T>(other);
            return otherHashset.IsSupersetOf(this);
        }

        public bool IsSupersetOf(IEnumerable<T> other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));
            return other.All(Contains);
        }

        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));
            var otherHashset = new HashSet<T>(other);
            return otherHashset.IsProperSubsetOf(this);
        }

        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));
            var otherHashset = new HashSet<T>(other);
            return otherHashset.IsProperSupersetOf(this);
        }

        public bool Overlaps(IEnumerable<T> other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));
            if (Count == 0) return false;
            return other.Any(Contains);
        }

        public bool SetEquals(IEnumerable<T> other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));
            var otherHashset = new HashSet<T>(other);
            return otherHashset.SetEquals(this);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
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

        public bool Equals(InsertionOrderedSet<T> other)
        {
            return other != null && SetEquals(other);
        }

        public override string ToString()
        {
            return this.ToVectorString();
        }
    }
}
