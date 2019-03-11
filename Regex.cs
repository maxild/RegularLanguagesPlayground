using System;
using System.Collections.Generic;

namespace RegExpToDfa
{
    // Abstract syntax of regular expressions
    //    r ::= A | r1 r2 | (r1|r2) | r*
    // NOTE: No concrete syntax is part of this project (yet...)

    /// <summary>
    /// Regular expressions
    /// </summary>
    public abstract class Regex
    {
        public abstract Nfa MkNfa(Nfa.NameSource names); // abstract factory for NFA-composite
    }

    /// <summary>
    /// Base (Atom) NFA for epsilon transition
    /// </summary>
    public class Eps : Regex
    {
        // The resulting nfa0 has form s0s -eps-> s0e

        public override Nfa MkNfa(Nfa.NameSource names)
        {
            int startState = names.Next();
            int exitState = names.Next();
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
        public override Nfa MkNfa(Nfa.NameSource names)
        {
            int startState = names.Next();
            int exitState = names.Next();
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
        public override Nfa MkNfa(Nfa.NameSource names)
        {
            Nfa nfa1 = _r1.MkNfa(names);
            Nfa nfa2 = _r2.MkNfa(names);
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

        public override Nfa MkNfa(Nfa.NameSource names)
        {
            Nfa nfa1 = _r1.MkNfa(names);
            Nfa nfa2 = _r2.MkNfa(names);
            int startState = names.Next();
            int exitState = names.Next();
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

        public override Nfa MkNfa(Nfa.NameSource names)
        {
            Nfa nfa1 = _r.MkNfa(names);
            int startState = names.Next();
            Nfa nfa0 = new Nfa(startState, startState);
            foreach (KeyValuePair<int, List<Transition>> entry in nfa1.Trans)
                nfa0.AddTrans(entry);
            nfa0.AddTrans(startState, null, nfa1.Start);
            nfa0.AddTrans(nfa1.GetRequiredSingleAcceptingState(), null, startState);
            return nfa0;
        }
    }
}
