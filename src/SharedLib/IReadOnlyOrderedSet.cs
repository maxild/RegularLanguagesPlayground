using System.Collections.Generic;

namespace AutomataLib
{
    public interface IReadOnlyOrderedSet<T> : IReadOnlySet<T>, IReadOnlyList<T>
    {
        int IndexOf(T item);
    }
}
