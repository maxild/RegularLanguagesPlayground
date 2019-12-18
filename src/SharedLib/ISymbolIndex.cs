using System;

namespace AutomataLib
{
    /// <summary>
    /// A named symbol that have a unique id that is designed to be used as an index,
    /// because it is sequentially ordered 0,1,2,...,N-1 across all possible symbols.
    /// </summary>
    public interface ISymbolIndex
        //where TEnum : struct, Enum
    {
        string Name { get; }

        int Index { get; }

        //TEnum Kind { get; }
    }
}
