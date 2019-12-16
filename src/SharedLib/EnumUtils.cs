using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public static IReadOnlyList<TIndexValue> MapToIndexValues<TEnum, TIndexValue>(Func<TEnum, string, int, TIndexValue> fn)
            where TEnum : struct, Enum
            where TIndexValue : IIndexerValue
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

            TIndexValue[] result = new TIndexValue[fields.Length];
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

                TIndexValue record = fn(enumValue, name, index);

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

            return length < fields.Length
                ? new ArraySegment<TIndexValue>(result, min, length)
                : result;
        }
    }
}
