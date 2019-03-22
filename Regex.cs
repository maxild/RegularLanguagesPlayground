using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace RegExpToDfa
{
    // ------------------------------------------------------------------------------------------------
    // Concrete syntax given with the following grammar that is based on the recursive definition of
    // regular expression
    //
    //      S -> S+S | SS | S* | (S)
    //      S -> c | 'ep' | 'empty'
    //
    // where 'c' is any character in the alphabet. {0,1} or {a,b} implies the following productions
    //
    //      S -> 0 | 1
    //      S -> a | b
    //
    // The above grammar is ambiguous, because the operators doesn't follow the precedence and
    // left-associative rules of the regular expression language.
    //
    // The correct (unambiguous) grammar is based on the following rules:
    //
    //     1) The unary star (Kleene closure) operator has the highest precedence and is left
    //        associative (that is the operator is on the right and binds to the left).
    //     2) Concatenation has the second highest precedence and is left associative.
    //     3) Union has the lowest precedence and is left associative.
    //
    // The following grammar capture the precedence and left associative rules
    //
    //    E -> E + T | T        (every union expression E is-a concat expression T)
    //    T -> TF | F           (every concat expression T is-a base expression F)
    //    F -> F* | P           (every base-expression F can be starred or wrap any other regular expression in parens)
    //    P -> (E) | 'c' | 'ep' | 'empty'
    //
    // The problem with this grammar is that it is left-recursive and therefore is not LL(1).
    // We can however convert it into an LL(1) grammar
    //
    //    E  -> TE'
    //    E' -> +TE' | epsilon
    //    T  -> FT'
    //    T' -> FT' | epsilon
    //    F  -> PF'
    //    F' -> *F' | epsilon
    //    P -> (E) | 'c' | 'eps' | 'empty'
    //
    // Terminals = {+, *, (, ), 'c', 'ep', 'empty'}
    //
    //
    // Any entry M(A, a) show us what the current variable (non-terminal) is expanded to during the
    // the left-most derivation (or equivalently build of the parse tree). If a cell is blank it is
    // a PARSE ERROR.
    //
    // LL(1) Parsing Table (Selections during left-most derivation is based on FIRST and FOLLOW sets).
    // -----------------------------------------------------------------------------------------
    //         ||    (    |    )    |   'c'   |  'eps'  | 'empty' |    +    |    *    |    $    |
    // -----------------------------------------------------------------------------------------
    //    E    ||   TE'   |         |   TE'   |   TE'   |   TE'   |         |         |         |
    //    E'   ||         | epsilon |         |         |         |  +TE'   |         | epsilon |
    //    T    ||   FT'   |         |   FT'   |   FT'   |   FT'   |         |         |         |
    //    T'   ||   FT'   | epsilon |   FT'   |   FT'   |   FT'   | epsilon |         | epsilon |
    //    F    ||   PF'   |         |   PF'   |   PF'   |   PF'   |         |         |         |
    //    F'   || epsilon | epsilon | epsilon | epsilon | epsilon | epsilon |   *F'   | epsilon |
    //    P    ||   (E)   |         |   'c'   |  'eps'  | 'empty' |         |         |         |
    //
    // ------------------------------------------------------------------------------------------------
    /// <summary>
    /// Textbook grammar used in 'Automata Theory' for RE.
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static class RegexTextbook
    {
        // We name/number Variables from 0, such that transitions can be stored in array
        private const char VAR_E = (char) 0;     // E
        private const char VAR_EP = (char) 1;    // E'
        private const char VAR_T = (char) 2;     // T
        private const char VAR_TP = (char) 3;    // T'
        private const char VAR_F = (char) 4;     // F
        private const char VAR_FP = (char) 5;    // F'
        private const char VAR_P= (char) 6;      // P

        private const char DOLLAR = '$';   // 0x24 == 36 (end of string)

        //
        // IMPORTANT: From here we define terminals (PROBLEM: dollar cannot be a terminal)
        //

        // could be '|', but we stick with the simple textbook version that doesn't require BNF notation of grammar
        private const char OP = '(';     // 0x28 == 40
        private const char CP = ')';     // 0x29 == 41
        private const char STAR = '*';   // 0x2A == 42
        private const char SUM = '+';    // 0x2B == 43
        private const char EPS = 'ε';    // 0x03B5 == \u03B5

        // TODO: alphabet hack...we only implement regular expressions over {a, b} alphabet
        private const char LETTER_a = 'a';
        private const char LETTER_b = 'b';
        private const char EMPTY = 'Ø'; // not very interesting in practice!!!!

        //
        // Other constants
        //

        // epsilon transition (removal of variable on the stack)
        private static readonly char [] NULLABLE = Array.Empty<char>(); // A -> epsilon

        /// <summary>
        /// Is this a non-terminal symbol?
        /// </summary>
        private static bool IsVariable(char grammarSymbol)
        {
            return grammarSymbol <= VAR_P;
        }

        private static bool IsTerminalOrDollar(char grammarSymbol)
        {
            return grammarSymbol > VAR_P; // IsNotVariable
        }

        /// <summary>
        /// Symbol to string
        /// </summary>
        private static string Text(char grammarSymbol)
        {
            switch (grammarSymbol)
            {
                case VAR_E: return "E";
                case VAR_EP: return "E'";
                case VAR_T: return "T";
                case VAR_TP: return "T'";
                case VAR_F: return "F";
                case VAR_FP: return "F'";
                case VAR_P: return "P";
                default: return new string(grammarSymbol, 1);
            }
        }

        /// <summary>
        /// stack to string (the part of the sentential form that have not been derived into terminal symbols)
        /// </summary>
        private static string Text(Stack<char> stack)
        {
            var sb = new StringBuilder();
            foreach (char c in stack)
            {
                sb.Append(Text(c));
            }
            return sb.ToString().PadLeft(20);
        }

        /// <summary>
        /// Input to string (the part of the input that have not been parsed)
        /// </summary>
        private static string Text(string re, int ip)
        {
            return ip < re.Length
                ? re.Substring(ip).PadLeft(10) + Text(DOLLAR)
                : string.Empty.PadLeft(10) + Text(DOLLAR);
        }

        /// <summary>
        /// Leave/terminal that is not an epsilon-production
        /// </summary>
        private static string PopText(char grammarSymbol)
        {
            return grammarSymbol == DOLLAR
                ? "ACCEPT"
                : "terminal";
        }

        private static char[] Prod(params char[] grammarSymbols)
        {
            return grammarSymbols;
        }

        // M[variable][terminal] is the production-RHS (body) to use for the given production-LHS (head)
        private static readonly IDictionary<char, char[]>[] M =
        {
            // E ->
            new Dictionary<char, char[]>
            {
                {OP, Prod(VAR_T, VAR_EP)},
                {LETTER_a, Prod(VAR_T, VAR_EP)},
                {LETTER_b, Prod(VAR_T, VAR_EP)},
                {EPS, Prod(VAR_T, VAR_EP)},
                {EMPTY, Prod(VAR_T, VAR_EP)}
            },
            // E' ->
            new Dictionary<char, char[]>
            {
                {CP, NULLABLE},
                {SUM, Prod(SUM, VAR_T, VAR_EP)},
                {DOLLAR, NULLABLE}
            },
            // T ->
            new Dictionary<char, char[]>
            {
                {OP, Prod(VAR_F, VAR_TP)},
                {LETTER_a, Prod(VAR_F, VAR_TP)},
                {LETTER_b, Prod(VAR_F, VAR_TP)},
                {EPS, Prod(VAR_F, VAR_TP)},
                {EMPTY, Prod(VAR_F, VAR_TP)}
            },
            // T' ->
            new Dictionary<char, char[]>
            {
                {OP, Prod(VAR_F, VAR_TP)},
                {CP, NULLABLE},
                {LETTER_a, Prod(VAR_F, VAR_TP)},
                {LETTER_b, Prod(VAR_F, VAR_TP)},
                {EPS, Prod(VAR_F, VAR_TP)},
                {EMPTY, Prod(VAR_F, VAR_TP)},
                {SUM, NULLABLE},
                {DOLLAR, NULLABLE}
            },
            // F ->
            new Dictionary<char, char[]>
            {
                {OP, Prod(VAR_P, VAR_FP)},
                {LETTER_a, Prod(VAR_P, VAR_FP)},
                {LETTER_b, Prod(VAR_P, VAR_FP)},
                {EPS, Prod(VAR_P, VAR_FP)},
                {EMPTY, Prod(VAR_P, VAR_FP)}
            },
            // F' ->
            new Dictionary<char, char[]>
            {
                {OP, NULLABLE},
                {CP, NULLABLE},
                {LETTER_a, NULLABLE},
                {LETTER_b, NULLABLE},
                {EPS, NULLABLE},
                {EMPTY, NULLABLE},
                {SUM, NULLABLE},
                {STAR, Prod(STAR, VAR_FP)},
                {DOLLAR, NULLABLE}
            },
            // P ->
            new Dictionary<char, char[]>
            {
                {OP, Prod(OP, VAR_E, CP)},
                {LETTER_a, Prod(LETTER_a)},
                {LETTER_b, Prod(LETTER_b)},
                {EPS, Prod(EPS)},
                {EMPTY, Prod(EMPTY)}
            }
        };

        /// <summary>
        /// Table driven LL(1) parser
        /// </summary>
        public static void Parse(string re)
        {
            var stack = new Stack<char>();
            stack.Push(DOLLAR); // add 'end-of-string' marker
            stack.Push(VAR_E);  // add start variable (variables are sort of like states)

            int ip = 0;

            while (true)
            {
                char grammarSymbol = stack.Peek();
                char symbol = ip < re.Length ? re[ip] : DOLLAR;

                if (IsTerminalOrDollar(grammarSymbol))
                {
                    if (grammarSymbol == symbol)
                    {
                        // ACTION: Show stack and input before we any of them
                        Debug.Write($"{Text(stack)} {Text(re, ip)}");

                        // pop terminal from stack and advance ip
                        stack.Pop();
                        ip += 1;

                        // ACTION: Output that we have reached a terminal (i.e. leave node)
                        Debug.WriteLine($"    {PopText(grammarSymbol)}");
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            $"The current input '{symbol}' cannot be matched with the current terminal symbol '{grammarSymbol}'.");
                    }
                }
                else
                {
                    // symbol is a variable, find the left-most derivation in the table
                    if (M[grammarSymbol].TryGetValue(symbol, out var body))
                    {
                        // ACTION: Show stack and input before we any of them
                        Debug.Write($"{Text(stack)} {Text(re, ip)}");

                        // pop variable of the stack
                        stack.Pop();

                        if (body.Length > 0)
                        {
                            // push new grammar symbols onto the stack in reverse order, such
                            // that first symbol is handled first (LIFO)
                            for (int i = body.Length - 1; i >= 0; i -= 1)
                            {
                                stack.Push(body[i]);
                            }

                            // ACTION: Output the production
                            Debug.Write($"    {Text(grammarSymbol)} -> ");
                            for (int i = 0; i < body.Length; i += 1)
                            {
                                Debug.Write(Text(body[i]));
                            }
                            Debug.WriteLine("");
                        }
                        else
                        {
                            // ACTION: Output the epsilon derivation, that signals that
                            // no new grammar symbols was pushed onto the stack
                            Debug.WriteLine($"    {Text(grammarSymbol)} -> {Text(EPS)}");
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException("No transition.");
                    }
                }

                if (grammarSymbol == DOLLAR)
                {
                    // we are done
                    break; // ACCEPT
                }
            }
        }

        // Non recursive transitions from table in state-machine
        public static Nfa Parse2(string re)
        {
            return null;
        }


        // recursive descent (inner functions for every variable)
        public static Nfa ParseRD(string re)
        {
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
