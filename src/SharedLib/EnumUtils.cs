using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace AutomataLib
{
    public static class EnumUtils
    {
        /// <summary>
        /// Map an enumeration to read-only list of sequentially ordered values (i.e. a contiguous range of index values)
        /// </summary>
        /// <remarks>
        /// The enumeration of named constants must have values that satisfy the following requirements
        ///   * Int32 values
        ///   * No duplicate values.
        ///   * Contiguous values 0,1,2,...,N-1
        ///   * [Flags] no accepted.
        /// </remarks>
        public static SymbolCache<TEnum, TSymbol> MapToSymbolCache<TEnum, TSymbol>(Func<string, int, TEnum, TSymbol> fn)
            where TEnum : struct, Enum
            where TSymbol : ISymbolIndex
        {
            // We have to use reflection APIs
            Type enumType = typeof(TEnum);

            // Underlying type must be Int32
            if (Type.GetTypeCode(enumType) != TypeCode.Int32)
                throw new InvalidOperationException(
                    $"Only enums with an underlying type of System.Int32 are supported --- check the specification of {enumType.FullName}.");

            // Enum cannot be [Flags]-enum
            if (enumType.IsDefined(typeof(FlagsAttribute), false))
                throw new InvalidOperationException($"Flags enums are not supported --- check the specification of {enumType.FullName}.");

            var fields = enumType.GetFields(BindingFlags.Public | BindingFlags.Static);

            int min = int.MaxValue;
            int max = int.MinValue;
            int length = 0;

            TSymbol[] result = new TSymbol[fields.Length];
            int[] dups = new int[fields.Length];

            for (int i = 0; i < fields.Length; i += 1)
            {
                var field = fields[i];
                var name = field.Name;

                // The underlying value
                int index = (int) field.GetValue(null);

                // Ignore negative values (i.e. negative values can have duplicates and be non-contiguous)
                if (index < 0) continue;

                // Values form a contiguous range.
                if (index >= dups.Length)
                    throw new InvalidOperationException(
                        $"The non-negative values must form a contiguous range 0,1,2,...,N-1 --- check the specification of {enumType.FullName}.");

                // No duplicate values
                if (dups[index] > 0)
                    throw new InvalidOperationException($"Duplicate values are not supported --- check the specification of {enumType.FullName}.");

                dups[index] = index + 1;

                if (index > max) max = index;
                if (index < min) min = index;

                // convert value to enum
                TEnum enumValue = Unsafe.As<int, TEnum>(ref index);

                TSymbol record = fn(name, index, enumValue);

                result[index] = record;

                length += 1;
            }

            // Values have min value zero.
            if (min != 0)
                throw new InvalidOperationException(
                    $"The non-negative values must have lower bound equal to zero --- check the specification of {enumType.FullName}.");

            // Values form a contiguous range.
            Debug.Assert(length == max + 1);
            //if (length != max + 1)
            //    throw new InvalidOperationException(
            //        $"The non-negative values must form a contiguous range 0,1,2,...,N-1 --- check the specification of {enumType.FullName}.");

            return new SymbolCache<TEnum, TSymbol>(
                length < fields.Length
                    ? new ArraySegment<TSymbol>(result, min, length)
                    : result);
        }
    }

    public class SymbolCache<TEnum, TSymbol> : IReadOnlyOrderedList<TSymbol>
        where TEnum : struct, Enum
        where TSymbol : ISymbolIndex
    {
        private readonly IReadOnlyList<TSymbol> _table;
        private readonly Dictionary<string, int> _index;

        public SymbolCache(IReadOnlyList<TSymbol> symbols)
        {
            _table = symbols ?? throw new ArgumentNullException(nameof(symbols));
            _index = symbols.ToDictionary(s => s.Name, s => s.Index);
        }

        public int MinIndex => 0;

        public int MaxIndex => _table.Count - 1;

        public int Count => _table.Count;

        public int IndexOf(TEnum index) => Unsafe.As<TEnum, int>(ref index);

        public int IndexOf(string name) => _index.TryGetValue(name, out int index) ? index : -1;

        // NOTE: Because TSymbol does not always ensure that symbol kind of TEnum is the same, we have to invoke Contains
        int IReadOnlyOrderedList<TSymbol>.IndexOf(TSymbol symbol) => Contains(symbol) ? symbol.Index : -1;

        public TSymbol this[TEnum index] => _table[IndexOf(index)];

        public TSymbol this[int index] => _table[index];

        public TSymbol this[string name] => _table[IndexOf(name)];

        // TODO: Ensure that indexers can be overloaded with different return types

        public IReadOnlyList<TSymbol> this[params TEnum[] indices] =>
            indices.Select(index => _table[IndexOf(index)]).ToArray();

        public IReadOnlyList<TSymbol> this[params string[] names] =>
            names.Select(name => _table[IndexOf(name)]).ToArray();

        // NOTE: Because TSymbol does not always ensure that symbol kind of TEnum is the same, we have to invoke Equals
        // TODO: Possible NullReferenceException
        public bool Contains(TSymbol symbol) => symbol.Index < _table.Count && _table[symbol.Index].Equals(symbol);

        public bool Contains(string name) => _index.ContainsKey(name);

        public IEnumerator<TSymbol> GetEnumerator()
        {
            return _table.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

}
