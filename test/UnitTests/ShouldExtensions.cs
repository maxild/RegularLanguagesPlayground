using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using AutomataLib;
using Xunit.Sdk;

namespace UnitTests
{
    public static class ShouldExtensions
    {
        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        public static void ShouldSetEqual<T>(this IEnumerable<T> actual, IEnumerable<T> expected) where T : IEquatable<T>
        {
            var actualProxy = GetProxy(actual);
            if (actualProxy.SetEquals == null)
                actualProxy = GetProxy(new HashSet<T>(actual));

            var expectedProxy = GetProxy(expected);
            if (expectedProxy.SetEquals == null)
                expectedProxy = GetProxy(new HashSet<T>(expected));

            if (false == actualProxy.SetEquals(expected))
            {
                var onlyInActual = new List<T>();
                foreach (T x in actual)
                    if (!expectedProxy.Contains(x))
                        onlyInActual.Add(x);

                var onlyInExpected = new List<T>();
                foreach (T x in expected)
                    if (!actualProxy.Contains(x))
                        onlyInExpected.Add(x);

                throw new AssertActualExpectedException(
                    expected.ToVectorString(maxElems: 10),
                    actual.ToVectorString(maxElems: 10),
                    $"The two sets Exp and Act are not equal:{Environment.NewLine}Exp\\Act = {onlyInExpected.ToVectorString()}{Environment.NewLine}Act\\Exp = {onlyInActual.ToVectorString()}");
            }

            static (Func<IEnumerable<T>, bool> SetEquals, Func<T, bool> Contains) GetProxy(IEnumerable<T> items)
            {
                return items switch
                {
                    IReadOnlySet<T> set => (set.SetEquals, set.Contains),
                    System.Collections.Generic.ISet<T> bclSet => (bclSet.SetEquals, bclSet.Contains),
                    _ => ((Func<IEnumerable<T>, bool>) null, (Func<T, bool>) null)
                };
            }
        }
    }
}
