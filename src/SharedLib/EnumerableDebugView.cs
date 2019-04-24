using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace AutomataLib
{
    internal class EnumerableDebugView<T>
    {
        private readonly IEnumerable<T> _items;

        public EnumerableDebugView(IEnumerable<T> items)
        {
            _items = items ?? throw new ArgumentNullException(nameof(items));
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items => _items.ToArray();
    }
}
