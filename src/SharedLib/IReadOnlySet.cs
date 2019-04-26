using System.Collections.Generic;

namespace AutomataLib
{
    public interface IReadOnlySet<T> : IReadOnlyCollection<T>
    {
        bool Contains(T item);

        bool IsSubsetOf(IEnumerable<T> other);

        bool IsSupersetOf(IEnumerable<T> other);

        bool IsProperSupersetOf(IEnumerable<T> other);

        bool IsProperSubsetOf(IEnumerable<T> other);

        bool Overlaps(IEnumerable<T> other);

        bool SetEquals(IEnumerable<T> other);
    }
}