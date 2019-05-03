using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using FiniteAutomata;

namespace CLI.TestDriver.Parsers
{
    // ------------------------------------------------------------------------------------------------
    // Concrete syntax given with the following grammar that is based on the recursive definition of
    // regular expression
    //
    //      S → S+S | SS | S* | (S)
    //      S → c | 'ep' | 'empty'
    //
    // where 'c' is any character in the alphabet. {0,1} or {a,b} implies the following productions
    //
    //      S → 0 | 1
    //      S → a | b
    //
    // The above grammar is ambiguous, because the operators doesn't follow the precedence and
    // left-associative rules of the regular expression language.
    //
    // The correct (unambiguous) grammar is based on the following rules:
    //
    //     1) The unary star (Kleene closure, iteration) operator has the highest precedence and is left
    //        associative (that is the operator is on the right and binds to the left).
    //     2) Concatenation has the second highest precedence and is left associative.
    //     3) Union has the lowest precedence and is left associative.
    //
    // The following grammar capture the precedence and left associative rules
    //
    //    E → E + T | T        (every union expression E is-a concat expression T)
    //    T → TF | F           (every concat expression T is-a base expression F)
    //    F → F* | P           (every base-expression F can be starred or wrap any other regular expression in parens)
    //    P → (E) | 'c' | 'ep' | 'empty'
    //
    // The problem with this grammar is that it is left-recursive and therefore is not LL(1).
    // We can however convert it into an LL(1) grammar
    //
    //    E  → TE'
    //    E' → +TE' | epsilon
    //    T  → FT'
    //    T' → FT' | epsilon
    //    F  → PF'
    //    F' → *F' | epsilon
    //    P → (E) | 'c' | 'eps' | 'empty'
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

    // TODO: Parse method should construct parse tree (concrete syntax tree), but we do not need C# types for the tree!!!
    // TODO: The only use of the parse tree is to create DOT graphviz spec such that the tree can be rendered.
    //       Maybe (left-most) derivation can be saved such that (DOT) tree can be created from
    //       those instructions without defining tree by defining C# types and instances. A derivation is
    //       simply a path through the LL(1) parse table. This can be saved into an array/list of productions used.
    // TODO: Parse method should construct abstract syntax tree (make ToDotLanguage on Regex and Dfa, and maybe Nfa)

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
        //private const char EMPTY = 'Ø'; // not very interesting in practice!!!!

        //
        // Other constants
        //

        // epsilon transition (removal of variable on the stack)
        private static readonly char [] NULLABLE = Array.Empty<char>(); // A → epsilon

        ///// <summary>
        ///// Is this a non-terminal symbol?
        ///// </summary>
        //private static bool IsVariable(char grammarSymbol)
        //{
        //    return grammarSymbol <= VAR_P;
        //}

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
            // E →
            new Dictionary<char, char[]>
            {
                {OP, Prod(VAR_T, VAR_EP)},
                {LETTER_a, Prod(VAR_T, VAR_EP)},
                {LETTER_b, Prod(VAR_T, VAR_EP)},
                {EPS, Prod(VAR_T, VAR_EP)}
            },
            // E' →
            new Dictionary<char, char[]>
            {
                {CP, NULLABLE},
                {SUM, Prod(SUM, VAR_T, VAR_EP)},
                {DOLLAR, NULLABLE}
            },
            // T →
            new Dictionary<char, char[]>
            {
                {OP, Prod(VAR_F, VAR_TP)},
                {LETTER_a, Prod(VAR_F, VAR_TP)},
                {LETTER_b, Prod(VAR_F, VAR_TP)},
                {EPS, Prod(VAR_F, VAR_TP)}
            },
            // T' →
            new Dictionary<char, char[]>
            {
                {OP, Prod(VAR_F, VAR_TP)},
                {CP, NULLABLE},
                {LETTER_a, Prod(VAR_F, VAR_TP)},
                {LETTER_b, Prod(VAR_F, VAR_TP)},
                {EPS, Prod(VAR_F, VAR_TP)},
                {SUM, NULLABLE},
                {DOLLAR, NULLABLE}
            },
            // F →
            new Dictionary<char, char[]>
            {
                {OP, Prod(VAR_P, VAR_FP)},
                {LETTER_a, Prod(VAR_P, VAR_FP)},
                {LETTER_b, Prod(VAR_P, VAR_FP)},
                {EPS, Prod(VAR_P, VAR_FP)}
            },
            // F' →
            new Dictionary<char, char[]>
            {
                {OP, NULLABLE},
                {CP, NULLABLE},
                {LETTER_a, NULLABLE},
                {LETTER_b, NULLABLE},
                {EPS, NULLABLE},
                {SUM, NULLABLE},
                {STAR, Prod(STAR, VAR_FP)},
                {DOLLAR, NULLABLE}
            },
            // P →
            new Dictionary<char, char[]>
            {
                {OP, Prod(OP, VAR_E, CP)},
                {LETTER_a, Prod(LETTER_a)},
                {LETTER_b, Prod(LETTER_b)},
                {EPS, Prod(EPS)}
            }
        };

        // TODO: Make the table-driven parser build the tree (Regex AST tree)

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
                    // TODO: Should we special case $ here
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
                            Debug.Write($"    {Text(grammarSymbol)} → ");
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
                            Debug.WriteLine($"    {Text(grammarSymbol)} → {Text(EPS)}");
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

        /// <summary>
        /// Recursive Descent Parser that builds AST (Regex composite that
        /// can be transformed to epsilon-NFA).
        /// </summary>
        public static Regex ParseRD(string re)
        {
            int ip = 0;
            char input = ip < re.Length ? re[ip] : DOLLAR;

            return ParseS();

            // S → E$ (clever start symbol that enforces $-end-of-string convention)
            Regex ParseS()
            {
                var regex = ParseE();
                Match(DOLLAR);
                return regex;
            }

            // E → TE'
            Regex ParseE()
            {
                // Is input in FIRST(E)
                if (input == OP || input == LETTER_a || input == LETTER_b || input == EPS)
                {
                    var regex = ParseT();
                    return ParseEP(regex);
                }
                else
                {
                    throw new InvalidOperationException(
                        $"The current input '{input}' cannot be matched with any symbol in SELECT(E).");
                }
            }

            // optional part of E (union expression, if any SUM terminal is found)
            // E' → +TE' | epsilon
            Regex ParseEP(Regex lhs)
            {
                // Is input in FIRST(E')
                if (input == SUM)
                {
                    Match(SUM);
                    var rhs = ParseT();
                    var union = new Alt(lhs, rhs);
                    return ParseEP(union); // left associative
                }
                else if (input == CP || input == DOLLAR)
                {
                    // epsilon-production
                    return lhs;
                }
                else
                {
                    throw new InvalidOperationException(
                        $"The current input '{input}' cannot be matched with any symbol in SELECT(E').");
                }
            }

            // T → FT'
            Regex ParseT()
            {
                // Is input in FIRST(T)
                if (input == OP || input == LETTER_a || input == LETTER_b || input == EPS)
                {
                    var lhs = ParseF();
                    return ParseTP(lhs);
                }
                else
                {
                    throw new InvalidOperationException(
                        $"The current input '{input}' cannot be matched with any symbol in SELECT(T).");
                }
            }

            // optional part of T (product expression, if any terminal to be concatenated is found)
            // T' → FT' | epsilon
            Regex ParseTP(Regex lhs)
            {
                // Is input in FIRST(TP)
                if (input == OP || input == LETTER_a || input == LETTER_b || input == EPS)
                {
                    // concatenation
                    var rhs = ParseF();
                    var product = new Seq(lhs, rhs);
                    return ParseTP(product);
                }
                // Is input in FOLLOW(TP)
                else if (input == CP || input == SUM || input == DOLLAR)
                {
                    // epsilon-production
                    return lhs;
                }
                else
                {
                    throw new InvalidOperationException(
                        $"The current input '{input}' cannot be matched with any symbol in SELECT(T').");
                }
            }

            // F → PF'
            Regex ParseF()
            {
                // Is input in FIRST(F)
                if (input == OP || input == LETTER_a || input == LETTER_b || input == EPS)
                {
                    // create new base expression
                    var regex = ParseP();
                    // see if Kleene closure star (iteration operator) is next input
                    return ParseFP(regex);
                }
                else
                {
                    throw new InvalidOperationException(
                        $"The current input '{input}' cannot be matched with any symbol in SELECT(F).");
                }
            }

            // optional part of Factor
            // F' → *F' | epsilon
            Regex ParseFP(Regex lhs)
            {
                if (input == STAR)
                {
                    // TODO: a** == a*
                    Match(STAR);
                    var star = new Star(lhs);
                    return ParseFP(star);
                }
                else if (input == OP || input == CP || input == LETTER_a || input == LETTER_b ||
                         input == EPS || input == SUM || input == DOLLAR)
                {
                    // epsilon production
                    return lhs;
                }
                else
                {
                    throw new InvalidOperationException(
                        $"The current input '{input}' cannot be matched with any symbol in SELECT(F').");
                }
            }

            // P → (E) | a | b | 'ep'
            Regex ParseP()
            {
                if (input == OP)
                {
                    Match(OP);
                    var regex = ParseE(); // new context
                    Match(CP);
                    return regex;
                }
                else if (input == LETTER_a)
                {
                    Match(LETTER_a);
                    return new Sym(Text(LETTER_a));
                }
                else if (input == LETTER_b)
                {
                    Match(LETTER_b);
                    return new Sym(Text(LETTER_b));
                }
                else if (input == EPS)
                {
                    Match(EPS);
                    return new Eps();
                }
                else
                {
                    throw new InvalidOperationException(
                        $"The current input '{input}' cannot be matched with any symbol in SELECT(P).");
                }
            }

            void Match(char terminal)
            {
                if (input == terminal)
                {
                    Debug.WriteLine($"We matched the terminal '{terminal}'");
                    ip += 1;
                    input = ip < re.Length ? re[ip] : DOLLAR;
                }
                else
                {
                    throw new InvalidOperationException($"We could not match the input symbol '{input}' with the terminal '{terminal}'.");
                }
            }
        }
    }
}
