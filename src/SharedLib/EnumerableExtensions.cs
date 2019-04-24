using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutomataLib
{
    public static class EnumerableExtensions
    {
        public static string ToVectorString<T>(this IEnumerable<T> values, int maxElems = 5)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }
            if (maxElems < 1)
            {
                throw new ArgumentException("The maximal number of elements cannot be less than one.");
            }

            int firstElems = maxElems / 2;
            int lastElems = maxElems / 2;
            if (firstElems + lastElems < maxElems)
            {
                firstElems += 1; // first elements are (a little) more important to show
            }

            var sb = new StringBuilder();

            var array = values.ToArray();

            sb.Append("{");

            if (array.Length > maxElems)
            {
                // show first elements
                sb.Append(array[0]);
                for (int i = 1; i < firstElems; i += 1)
                {
                    sb.Append(", ");
                    sb.Append(array[i]);
                }
                // show elipsis indicating that not all elements have been shown
                sb.Append(",...");
                // show last elements
                for (int i = array.Length - lastElems; i < array.Length; i += 1)
                {
                    sb.Append(", ");
                    sb.Append(array[i]);
                }
            }
            else
            {
                // show all elements
                for (int i = 0; i < array.Length; i += 1)
                {
                    if (i > 0)
                    {
                        sb.Append(", ");
                    }
                    sb.Append(array[i]);
                }
            }

            sb.Append("}");

            return sb.ToString();
        }
    }
}
