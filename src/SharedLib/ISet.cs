using System.Collections.Generic;

namespace AutomataLib
{
    /// <summary>
    /// Minimal Set API
    /// </summary>
    public interface ISet<T> : IReadOnlySet<T>
    {
        bool Add(T item);

        bool AddRange(IEnumerable<T> other);
    }
}
