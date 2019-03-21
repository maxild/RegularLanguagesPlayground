using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace RegExpToDfa
{
    // ------------------------------------------------------------------------------------------------
    // Concrete syntax given with the following (ambiguous and left-recursive)
    // grammar
    //       R -> R+R | RR | R* | (R) | c | 'ep'   (where 'c' is any character, i.e. the alphabet)
    // The equivalent unambiguous grammar, where precedence is given from lowest to highest by
    //     1) The unary star (Kleene closure) operator has the highest precedence and is left
    //        associative (that is the operator is on the right and bind to the left).
    //     2) Concatenation has the second highest precedence and is left associative.
    //     3) Union has the lowest precedence and is left associative.
    // The following (unambiguous) grammar capture the precedence and left associative rules
    //    R -> R + E | E                    (every union expression is-a concat expression)
    //    E -> ET | T                       (every concat expression is-a star expression)
    //    T -> F* | F                       (every star expression is-a group or base expression)
    //    F  -> c | (R) | 'ep'
    // The problem with this grammar is that it is left-recursive (because of left associative rules).
    // We can convert it into the following (less logical/understandable grammar) that is suitable for
    // easy predictive parsing (LL(1) grammar)
    //    R  -> ER'
    //    R' -> +ER' | epsilon
    //    E  -> TE'
    //    E' -> TE' | epsilon
    //    T  -> (R)T' | cT' | 'ep'T'
    //    T' -> *T' | epsilon
    //
    // Alternate formulation (S=start, U=union, C=concatenation, K=kleene-star, B=basis-or-sub)
    // TODO: maybe use other letters for non-terminals
    // TODO: Calculate NULLABLE, FIRST and FOLLOW sets for all productions
    //    S -> U
    //    U  -> CU'
    //    U' -> +CU' | epsilon
    //    C  -> KC'
    //    C' -> KC' | epsilon
    //    K  -> BK'
    //    K' -> *K' | epsilon
    //    B  -> (U) | 'c' | 'ep'
    // Terminals = {+, *, (, ), 'c', 'ep'},    decide what 'c' is?, maybe {a, b} or {0, 1}???
    // ------------------------------------------------------------------------------------------------
    /// <summary>
    /// Textbook grammar used in 'Automata Theory' for RE.
    /// </summary>
    public static class RegexTextbook
    {
        // could be '|', but we stick with the simple textbook version that doesn't require BNF notation of grammar
        private const string OR = "+";
        private const string STAR = "*";
        private const string OP = "(";
        private const string CP = ")";
        private const string EPS = "ε"; // Unix shorthand notation makes EPS token (terminal) redundant
        private const string EPS_U = "\u03B5"; // unicode 03B5 (not ASCII)

        public static Nfa Parse(string re)
        {
            Debug.Assert(EPS == EPS_U);
            return null;
        }
    }

    /// <summary>
    /// Abstract syntax of (textbook) regular expressions
    ///     Base case of single character symbols (denoted by 'c') or empty string (denoted by epsilon)
    ///         r ::= 'c' | epsilon
    ///     Suppose r and s are regular expressions for L(r) and L(s)
    ///         r ::= r|s | rs | (r)* | (r)
    /// </summary>
    public abstract class Regex
    {
        public abstract Nfa MkNfa(Func<int> nameFunc); // abstract factory for NFA-composite
    }

    /// <summary>
    /// Base (Atom) NFA for epsilon transition
    /// </summary>
    public class Eps : Regex
    {
        // The resulting nfa0 has form s0s -eps-> s0e

        public override Nfa MkNfa(Func<int> nameFunc)
        {
            int startState = nameFunc();
            int exitState = nameFunc();
            Nfa nfa0 = new Nfa(startState, exitState);
            nfa0.AddTrans(startState, null, exitState);
            return nfa0;
        }
    }

    /// <summary>
    /// Base (Atom) NFA for single character transition
    /// </summary>
    public class Sym : Regex
    {
        readonly string _sym;

        // TODO: Why not use char? in transition and char here?
        public Sym(string sym)
        {
            if (sym == null) throw new ArgumentNullException(nameof(sym));
            if (sym.Length != 1) throw new ArgumentException("Sym must contain single character string");
            _sym = sym;
        }

        // The resulting nfa0 has form s0s -sym-> s0e
        public override Nfa MkNfa(Func<int> nameFunc)
        {
            int startState = nameFunc();
            int exitState = nameFunc();
            Nfa nfa = new Nfa(startState, exitState);
            nfa.AddTrans(startState, _sym, exitState);
            return nfa;
        }
    }

    /// <summary>
    /// Concatenation operator NFA builder
    /// </summary>
    public class Seq : Regex
    {
        private readonly Regex _r1;
        private readonly Regex _r2;

        public Seq(Regex r1, Regex r2)
        {
            _r1 = r1;
            _r2 = r2;
        }

        // If   nfa1 has form s1s ----> s1e
        // and  nfa2 has form s2s ----> s2e
        // then nfa0 has form s1s ----> s1e -eps-> s2s ----> s2e
        public override Nfa MkNfa(Func<int> nameFunc)
        {
            Nfa nfa1 = _r1.MkNfa(nameFunc);
            Nfa nfa2 = _r2.MkNfa(nameFunc);
            Nfa nfa0 = new Nfa(nfa1.Start, nfa2.GetRequiredSingleAcceptingState());
            foreach (KeyValuePair<int, List<Transition>> entry in nfa1.Trans)
                nfa0.AddTrans(entry);
            foreach (KeyValuePair<int, List<Transition>> entry in nfa2.Trans)
                nfa0.AddTrans(entry);
            nfa0.AddTrans(nfa1.GetRequiredSingleAcceptingState(), null, nfa2.Start);
            return nfa0;
        }
    }

    /// <summary>
    /// Union NFA builder
    /// </summary>
    public class Alt : Regex
    {
        private readonly Regex _r1;
        private readonly Regex _r2;

        public Alt(Regex r1, Regex r2)
        {
            _r1 = r1;
            _r2 = r2;
        }

        // If   nfa1 has form s1s ----> s1e
        // and  nfa2 has form s2s ----> s2e
        // then nfa0 has form s0s -eps-> s1s ----> s1e -eps-> s0e
        //                    s0s -eps-> s2s ----> s2e -eps-> s0e

        public override Nfa MkNfa(Func<int> nameFunc)
        {
            Nfa nfa1 = _r1.MkNfa(nameFunc);
            Nfa nfa2 = _r2.MkNfa(nameFunc);
            int startState = nameFunc();
            int exitState = nameFunc();
            Nfa nfa0 = new Nfa(startState, exitState);
            foreach (KeyValuePair<int, List<Transition>> entry in nfa1.Trans)
                nfa0.AddTrans(entry);
            foreach (KeyValuePair<int, List<Transition>> entry in nfa2.Trans)
                nfa0.AddTrans(entry);
            nfa0.AddTrans(startState, null, nfa1.Start);
            nfa0.AddTrans(startState, null, nfa2.Start);
            nfa0.AddTrans(nfa1.GetRequiredSingleAcceptingState(), null, exitState);
            nfa0.AddTrans(nfa2.GetRequiredSingleAcceptingState(), null, exitState);
            return nfa0;
        }
    }

    /// <summary>
    /// Kleene Star NFA builder
    /// </summary>
    public class Star : Regex
    {
        private readonly Regex _r;

        public Star(Regex r)
        {
            _r = r;
        }

        // If   nfa1 has form s1s ----> s1e
        // then nfa0 has form s0s ----> s0s
        //                    s0s -eps-> s1s
        //                    s1e -eps-> s0s

        public override Nfa MkNfa(Func<int> nameFunc)
        {
            Nfa nfa1 = _r.MkNfa(nameFunc);
            int startState = nameFunc();
            Nfa nfa0 = new Nfa(startState, startState);
            foreach (KeyValuePair<int, List<Transition>> entry in nfa1.Trans)
                nfa0.AddTrans(entry);
            nfa0.AddTrans(startState, null, nfa1.Start);
            nfa0.AddTrans(nfa1.GetRequiredSingleAcceptingState(), null, startState);
            return nfa0;
        }
    }
}
