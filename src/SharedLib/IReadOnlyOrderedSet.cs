namespace AutomataLib
{
    /// <summary>
    /// Represents a read-only sequentially (insertion) ordered set of elements that can be accessed by index.
    /// </summary>
    public interface IReadOnlyOrderedSet<T> : IReadOnlySet<T>, IReadOnlyOrderedList<T>
    {
    }
}
