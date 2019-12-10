
using System.Collections.Generic;

namespace AutomataLib
{
    // Mathematical Concepts from Order and Lattice Theory
    // ====================================================
    // A set P equipped with a partial order, strict or non-strict, is called a poset (a partially ordered set).
    // In mathematical terms, an ordering is a binary relation on a set of objects. A pre-order has the following
    // properties
    //              (i)  Reflexive
    //              (ii) Transitive
    //
    // A partial order has a third property (antisymmetry for strict, and antisymmetri for non-strict case)
    //
    //              (iii strict)        antisymmetry: for all x, y ∈ P, if x < y holds, then y < x does not hold;
    //              (iii non-strict)    antisymmetry: for all x, y ∈ P, x <= y and y >= x imply x = y;
    //
    // A poset in which any two elements are comparable is called a chain, and the associated order relation is
    // called a total order.
    //
    // NOTE: strict and non-strict relation is two sides of the same coin, because
    //
    //              x <= y    iff    x < y  or x = y
    //
    //              R<=  =  R< ∪ R=, where R<=, R< and R= are the relations for <=, < and = respectively.
    //
    //              R<= := {(x, y) : x, y ∈ P, x <= y},       (non-strict relation)
    //              R<  := {(x, y) : x, y ∈ P, x < y},        (strict relation)
    //              R=  := {(x, x) : x ∈ P },                 (antisymmetry implies singleton equivalence sets)
    //
    // If < is a strict partial order on X, then <= defined by
    //
    //          x <= y  iff     x < y  or  x = y
    //
    // is a non-strict partial order on X.
    //
    //  If <= is a partial order on X, then < defined by
    //
    //          x < y   iff     x <= y and not (x = y)
    //
    // is a strict partial order on X.
    // The only partial order that is also an equivalence relation is equality (R= from above).
    //

    //
    //
    // For some given domain given by a finite X, many structures in discrete math (and programming, algorithms) are
    // members of the powerset of X. This powerset carries a natural ordering, namely set inclusion, ⊆. We denote
    // the set of all subsets of X by Pow(X), and always regard this as equipped with the inclusion order. Alternatively
    // (though this may seem perverse at this stage), we might order the subsets of X by reverse inclusion, ⊇.
    // When we wish to use reverse inclusion we shall write Pow(X)∂ (i.e. the dual of Pow(X) with reversed order).
    //
    // Given any poset P we can form a new poset P∂ (the dual of P) by defining x <= y to hold in P∂, if and only if,
    // y <= x holds in P. Any statement about a poset P yields a corresponding (dual) statement about P∂, obtained by
    // interchanging and  and making consequential changes to all other symbols. This Duality Principle permits us
    // to prove just one of any pair of mutually dual claims.
    //
    // We say P has a bottom element if there exists ⊥ ∈ P (called bottom) with the property that ⊥ <= x for all x ∈ P.
    // Dually, P has a top element if there exists T ∈ P such that x <= T for all x ∈ P. In (Pow(X), ⊆), we have ⊥ = Ø
    // and T = X.
    //
    // It should come as no surprise at all that along with posets we also consider suitable structure-preserving maps
    // between posets. Let P and Q be posets. A map F : P → Q is said to be
    //
    //      (i)   monotone (aka ab order-preserving map), if x <= y in P implies F(x) <= F(y) in Q
    //      (ii)  an order-embedding map, if x <= y in P iff F(x) <= F(y) in Q;
    //      (iii) an order-isomorphism map, if it is an order-embedding mapping P onto Q.
    //
    //
    // Let P be a poset.
    //
    //      (i)     Let x ∈ P. Then define ↑x := { y ∈ P : y >= x }.
    //      (ii)    Let Y ⊆ P. Then Y is an up-set of P if x ∈ P, x >= y, y ∈ Y implies x ∈ Y .
    //
    //      (iii)   Let x ∈ P. Then define ↓x := { y∈ P : y ≤ x}
    //      (iv)    Let...
    //
    // Let P be a poset and x, y ∈ P. Then we claim that the following are equivalent:
    //
    //      (a) x <= y
    //      (b) ↓x ⊆ ↓y
    //
    // May important properties of a poset P are expressed in terms of the existence of certain upper bounds or lower
    // bounds of subsets of P. Important classes of posets defined in this way are
    //          • lattices,
    //          • complete lattices,
    //          • CPOs (complete partial orders).
    //
    // Let L be a non-empty poset. Then L is a lattice if, for x, y ∈ L, there exists elements x ∨ y and x ∧ y in L
    // such that
    //
    //              ↑x ∩ ↑y = ↑(x ∨ y) and ↓x ∩ ↓y = ↓(x ∧ y)
    //
    // the elements x ∨ y and x ∧ y are called, respectively, the join (or supremum) and meet (or infimum) of x and y.
    // Formally, ∨: L×L → L and ∧: L×L → L are binary operations on L. Note that L∂ is a lattice if and only if L is,
    // with the roles of ∨ and ∧ swapping.
    //
    // As a special kind of poset, a lattice is equipped with a partial order, <= , as well as with the binary operations
    // of join and meet. The link between ∨, ∧ and <=, is given by
    //
    //                              x ∧ y = x  iff  x <= y   iff   x ∨ y = y
    // For any set X, the powerset Pow(X) is a lattice in which ∨ and ∧ are just ∪ and ∩. Dually, Pow(X)∂ is a lattice,
    // with ∨ as ∩ and ∧ as ∪.
    //
    // In any powerset Pow(X) we have, for A,B,C ⊆ X the following distributive laws,
    //
    //              A ∩ (B ∪ C) = (A ∩ B) ∪ (A ∩ C) and A ∪ (B ∩ C) = (A ∪ B) ∩ (A ∪ C).
    //
    // Therefore (Pow(X), ⊆) is a distributive lattice that satisfies for all x, y, z ∈ Pow(X)
    //
    //          (D)  x ∧ (y ∨ z) = (x ∧ y) ∨ (x ∧ z);
    //          (D)∂ x ∨ (y ∧ z) = (x ∨ y) ∧ (x ∨ z).
    //
    // Closure:
    // ========
    // Let X be a set and let L be a family of subsets of X, ordered as usual by inclusion, and such that
    //
    // TODO
    //
    // Then L is a complete lattice in which
    //
    //              ∧ i∈I A(i) = ∩ i∈I A(i),
    //              ∨ i∈I A(i) = ∩ {B ∈ L : U i∈I A(i) ⊆ B }.
    //
    // Fixed Points.and least Fixed Poins..TODO
    //      Knaster–Tarski Fixed Point Theorem
    //      Fixed Point Theorem for CPOs

    public interface IReadOnlySet<T> : IReadOnlyCollection<T>
    {
        bool Contains(T item);

        /// <summary>
        /// THIS ⊆ OTHER
        /// </summary>
        bool IsSubsetOf(IEnumerable<T> other);

        /// <summary>
        /// THIS ⊇ OTHER
        /// </summary>
        bool IsSupersetOf(IEnumerable<T> other);

        /// <summary>
        /// THIS ⊃ OTHER
        /// </summary>
        bool IsProperSupersetOf(IEnumerable<T> other);

        /// <summary>
        /// THIS ⊂ OTHER
        /// </summary>
        bool IsProperSubsetOf(IEnumerable<T> other);

        bool Overlaps(IEnumerable<T> other);

        bool SetEquals(IEnumerable<T> other);
    }
}
