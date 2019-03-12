using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace RegExpToDfa
{
    /// <summary>
    /// Pair/tuple that by convention has a smaller fst value than snd value.
    /// </summary>
    public struct TriangularPair<T> : IEquatable<TriangularPair<T>> where T : IEquatable<T>, IComparable<T>
    {
        public T Fst;
        public T Snd;

        public TriangularPair(T p, T q)
        {
            if (p.CompareTo(q) < 0)
            {
                Fst = p; Snd = q;
            }
            else
            {
                Fst = q; Snd = p;
            }
        }

        /// <summary>
        /// The pair contains any of the states for the other pair.
        /// </summary>
        //public bool IsEquivalentTo(TriangularPair<T> other)
        //{
        //    // By transitivity of equivalence the two pairs are in the same block of equivalent states
        //    return Fst.Equals(other.Fst) || Fst.Equals(other.Snd) || Snd.Equals(other.Fst) || Snd.Equals(other.Snd);
        //}

        public bool IsEqToBlock(Set<T> other)
        {
            // By transitivity of equivalence the two pairs are in the same block of equivalent states
            return other.Contains(Fst) || other.Contains(Snd);
        }

        /// <summary>
        /// The pair contains the state as either first or second element.
        /// </summary>
        public bool Contains(T s)
        {
            return Fst.Equals(s) || Snd.Equals(s);
        }

        public override string ToString()
        {
            return $"({Fst}, {Snd})";
        }

        public bool Equals(TriangularPair<T> other)
        {
            return EqualityComparer<T>.Default.Equals(Fst, other.Fst) &&
                   EqualityComparer<T>.Default.Equals(Snd, other.Snd);
        }

        public override bool Equals(object obj)
        {
            return obj is TriangularPair<T> other && Equals(other);
        }

        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode()
        {
            unchecked
            {
                return (EqualityComparer<T>.Default.GetHashCode(Fst) * 397) ^ EqualityComparer<T>.Default.GetHashCode(Snd);
            }
        }
    }
}
