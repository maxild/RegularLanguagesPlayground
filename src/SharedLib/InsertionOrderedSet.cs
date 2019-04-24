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
    public class InsertionOrderedSet<T> : ISet<T>, IEquatable<InsertionOrderedSet<T>> where T : IEquatable<T>
    {
        private string DebuggerDisplay => Count > 0
            ? $"{this.ToVectorString()}, Count = {Count}"
            : $"Count = {Count}";


        private readonly IDictionary<T, LinkedListNode<T>> _dictionary;
        private readonly LinkedList<T> _linkedList;
        private int? _cachedHash; // Cached hash code is valid if non-null (THIS IS SPECIAL)

        public InsertionOrderedSet()
            : this(EqualityComparer<T>.Default)
        {
        }

        public InsertionOrderedSet(IEqualityComparer<T> comparer)
        {
            _dictionary = new Dictionary<T, LinkedListNode<T>>(comparer);
            _linkedList = new LinkedList<T>();
        }

        public int Count => _dictionary.Count;

        public bool Add(T item)
        {
            if (_dictionary.ContainsKey(item)) return false;
            LinkedListNode<T> node = _linkedList.AddLast(item);
            _dictionary.Add(item, node);
            _cachedHash = null;
            return true;
        }

        public bool AddRange(IEnumerable<T> other)
        {
            bool added = false;
            foreach (var item in other)
                added = Add(item) || added;
            if (added)
                _cachedHash = null;
            return added;
        }

        //public void Clear()
        //{
        //    _linkedList.Clear();
        //    _dictionary.Clear();
        //    _cachedHash = null;
        //}

        //public bool Remove(T item)
        //{
        //    bool found = _dictionary.TryGetValue(item, out var node);
        //    if (!found) return false;
        //    _dictionary.Remove(item);
        //    _linkedList.Remove(node);
        //    return true;
        //}

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

        //public void CopyTo(T[] array, int arrayIndex)
        //{
        //    _linkedList.CopyTo(array, arrayIndex);
        //}

        public IEnumerator<T> GetEnumerator()
        {
            return _linkedList.GetEnumerator();
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
