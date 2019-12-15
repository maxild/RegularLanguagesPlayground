using System.Collections.Generic;

namespace AutomataLib
{
    /// <summary>
    /// Represents a read-only sequentially ordered collection of elements that can be accessed by index.
    /// </summary>
    public interface IReadOnlyOrderedList<T> : IReadOnlyList<T>
    {
        int IndexOf(T item);
    }
}
