using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using AutomataLib;
using CLI.TestDriver.Parsers;
using ContextFreeGrammar;
using FiniteAutomata;

namespace CLI.TestDriver
{
    static class Program
    {
        public static void Main()
        {
            //DragonBookEx4_48();
            //StanfordShiftReduceConflictGrammar();
            //StanfordReduceReduceConflictGrammar();
            //ExprGrammarStanfordNotesOnBottomUpParsing();
            //DanglingElseWhenParsing_iEtiEtSeS_ImpliesShiftReduceConflictAfterParsing_iEtiEtS_InState8();
            //ExprGrammarCh4DragonBook();
            //ToyExampleGrammarFromGallierNotesOnLr0Parsing();
            ToyExampleGrammarFromGallierNotesOnLr1Parsing();
            //ExprGrammarGallierNotesOnLrParsing();
            //CourseExercise();

            //RegexParser();
            //KeywordAutomata();
            //EquivalenceOfTwoDfas();
            //NonMinimalDfa();
            //DecimalAutomata();
        }

        //
        // Context-Free languages, CFG and LR Parsing
        //

        public static void ToyExampleGrammarFromGallierNotesOnLr1Parsing()
        {
            // 0: S → E
            // 1: E → aEb
            // 2: E → ε
            var grammar = new GrammarBuilder()
                .SetNonterminalSymbols(Symbol.Vs("S", "E"))
                .SetTerminalSymbols(Symbol.Ts('a', 'b'))
                .SetStartSymbol(Symbol.V("S"))
                .AndProductions(
                    Symbol.V("S").GoesTo(Symbol.V("E")),
                    Symbol.V("E").GoesTo(Symbol.T('a'), Symbol.V("E"), Symbol.T('b')),
                    Symbol.V("E").GoesTo(Symbol.Epsilon)
                );

            // Create NFA (digraph of items labeled by symbols)
            var characteristicStringsNfa = grammar.GetLr1AutomatonNfa();

            SaveFile("NCG1.dot", DotLanguagePrinter.ToDotLanguage(characteristicStringsNfa, DotRankDirection.TopBottom));

            var dfa = characteristicStringsNfa.ToDfa();

            SaveFile("DCG1.dot", DotLanguagePrinter.ToDotLanguage(dfa, DotRankDirection.LeftRight, skipStateLabeling:true));

            //var dfa2 = grammar.GetLr1AutomatonDfa();

            //SaveFile("DCG1Lr.dot", DotLanguagePrinter.ToDotLanguage(dfa2, DotRankDirection.LeftRight, skipStateLabeling:true));
        }

        public static void ToyExampleGrammarFromGallierNotesOnLr0Parsing()
        {
            // Augmented Grammar (assumed reduced, i.e. no useless symbols).
            //
            // ({S,E}, {a,b}, P, S) with P given by
            //
            // The purpose of this new starting production (S) is to indicate to the parser when
            // it should stop parsing and announce acceptance of input.
            //
            // 0: S → E
            // 1: E → aEb
            // 2: E → ab
            var grammar = new GrammarBuilder()
                .SetNonterminalSymbols(Symbol.Vs("S", "E"))
                .SetTerminalSymbols(Symbol.Ts('a', 'b'))
                .SetStartSymbol(Symbol.V("S"))
                .AndProductions(
                    Symbol.V("S").GoesTo(Symbol.V("E")),
                    Symbol.V("E").GoesTo(Symbol.T('a'), Symbol.V("E"), Symbol.T('b')),
                    Symbol.V("E").GoesTo(Symbol.T('a'), Symbol.T('b'))
                );

            // Create NFA (digraph of items labeled by symbols)
            var characteristicStringsNfa = grammar.GetLr0AutomatonNfa();

            SaveFile("NCG0.dot", DotLanguagePrinter.ToDotLanguage(characteristicStringsNfa, DotRankDirection.TopBottom));

            var dfa = characteristicStringsNfa.ToDfa();

            SaveFile("DCG0.dot", DotLanguagePrinter.ToDotLanguage(dfa, DotRankDirection.LeftRight, skipStateLabeling: true));

            var dfa2 = grammar.GetLr0AutomatonDfa();

            SaveFile("DCG0Lr.dot", DotLanguagePrinter.ToDotLanguage(dfa2, DotRankDirection.LeftRight, skipStateLabeling: true));
        }


        public static void ExprGrammarCh4DragonBook()
        {
            // NOTE: Augmented, but without explicit EOF symbol (the EOF marker is optional, but changes the number of states)
            // 0: S → E
            // 1: E → E + T
            // 2: E → T
            // 3: T → T * F
            // 4: T → F
            // 5: T → (E)
            // 6: T → a        (Dragon book has 'id' terminal here, but our model only supports single char tokens at the moment)
            var grammar = new GrammarBuilder()
                .SetNonterminalSymbols(Symbol.Vs("S", "E", "T", "F"))
                .SetTerminalSymbols(Symbol.Ts('a', '+', '*', '(', ')'))
                .SetStartSymbol(Symbol.V("S"))
                .AndProductions(
                    Symbol.V("S").GoesTo(Symbol.V("E")), //, Symbol.EofMarker),
                    Symbol.V("E").GoesTo(Symbol.V("E"), Symbol.T('+'), Symbol.V("T")),
                    Symbol.V("E").GoesTo(Symbol.V("T")),
                    Symbol.V("T").GoesTo(Symbol.V("T"), Symbol.T('*'), Symbol.V("F")),
                    Symbol.V("T").GoesTo(Symbol.V("F")),
                    Symbol.V("F").GoesTo(Symbol.T('('), Symbol.V("E"), Symbol.T(')')),
                    Symbol.V("F").GoesTo(Symbol.T('a'))
                );

            // Create NFA (digraph of items labeled by symbols)
            var characteristicStringsNfa = grammar.GetLr0AutomatonNfa();

            SaveFile("DragonNCG.dot", DotLanguagePrinter.ToDotLanguage(characteristicStringsNfa, DotRankDirection.TopBottom));

            var dfa = characteristicStringsNfa.ToDfa();

            SaveFile("DragonDCG.dot", DotLanguagePrinter.ToDotLanguage(dfa, DotRankDirection.LeftRight, skipStateLabeling:true));

            var dfa2 = grammar.GetLr0AutomatonDfa();

            SaveFile("DragonDCGLr.dot", DotLanguagePrinter.ToDotLanguage(dfa2, DotRankDirection.LeftRight, skipStateLabeling:true));

            grammar.ComputeSlrParsingTable().Parse("a*a+a", Console.Out); // TODO: Lexer should skip whitespace
        }

        /// <summary>
        /// Example 4.48 in Dragon book p. 255, 2nd ed
        /// </summary>
        public static void DragonBookEx4_48()
        {
            // NOTE: L =l-value, R = r-value, and * is the prefix operator for pointers (as known from C lang)
            // 0: S' → S
            // 1: S → L = R
            // 2: S → R
            // 3: L → *R
            // 4: L → a        (Dragon book has 'id' terminal here, but our model only supports single char tokens at the moment)
            // 5: R → L
            var grammar = new GrammarBuilder()
                .SetNonterminalSymbols(Symbol.Vs("S'", "S", "L", "R"))
                .SetTerminalSymbols(Symbol.Ts('a', '=', '*'))
                .SetStartSymbol(Symbol.V("S'"))
                .AndProductions(
                    Symbol.V("S'").GoesTo(Symbol.V("S")),
                    Symbol.V("S").GoesTo(Symbol.V("L"), Symbol.T('='), Symbol.V("R")),
                    Symbol.V("S").GoesTo(Symbol.V("R")),
                    Symbol.V("L").GoesTo(Symbol.T('*'), Symbol.V("R")),
                    Symbol.V("L").GoesTo(Symbol.T('a')),
                    Symbol.V("R").GoesTo(Symbol.V("L"))
                );

            // Create NFA (digraph of items labeled by symbols)
            var characteristicStringsNfa = grammar.GetLr0AutomatonNfa();

            SaveFile("DragonBookEx4_48_NCG.dot", DotLanguagePrinter.ToDotLanguage(characteristicStringsNfa, DotRankDirection.TopBottom));

            var dfa = characteristicStringsNfa.ToDfa();

            SaveFile("DragonBookEx4_48_DCG.dot", DotLanguagePrinter.ToDotLanguage(dfa, DotRankDirection.LeftRight, skipStateLabeling:true));

            var dfa2 = grammar.GetLr0AutomatonDfa();

            SaveFile("DragonBookEx4_48_DCGLr.dot", DotLanguagePrinter.ToDotLanguage(dfa2, DotRankDirection.LeftRight, skipStateLabeling:true));
        }


        public static void ExprGrammarGallierNotesOnLrParsing()
        {
            // 0: S → E
            // 1: E → E + T
            // 2: E → T
            // 3: T → T * a
            // 4: T → a
            var grammar = new GrammarBuilder()
                .SetNonterminalSymbols(Symbol.Vs("S", "E", "T"))
                .SetTerminalSymbols(Symbol.Ts('a', '+', '*'))
                .SetStartSymbol(Symbol.V("S"))
                .AndProductions(
                    Symbol.V("S").GoesTo(Symbol.V("E")),
                    Symbol.V("E").GoesTo(Symbol.V("E"), Symbol.T('+'), Symbol.V("T")),
                    Symbol.V("E").GoesTo(Symbol.V("T")),
                    Symbol.V("T").GoesTo(Symbol.V("T"), Symbol.T('*'), Symbol.T('a')),
                    Symbol.V("T").GoesTo(Symbol.T('a'))
                );

            // Create NFA (digraph of items labeled by symbols)
            var characteristicStringsNfa = grammar.GetLr0AutomatonNfa();

            SaveFile("NCG2.dot", DotLanguagePrinter.ToDotLanguage(characteristicStringsNfa, DotRankDirection.TopBottom));

            var dfa = characteristicStringsNfa.ToDfa();

            SaveFile("DCG2.dot", DotLanguagePrinter.ToDotLanguage(dfa, DotRankDirection.LeftRight, skipStateLabeling:true));

            var dfa2 = grammar.GetLr0AutomatonDfa();

            SaveFile("DCG2Lr.dot", DotLanguagePrinter.ToDotLanguage(dfa2, DotRankDirection.LeftRight, skipStateLabeling:true));
        }

        public static void ExprGrammarStanfordNotesOnBottomUpParsing()
        {
            // 0: S → E
            // 1: E → E + T
            // 2: E → T
            // 3: T → (E)
            // 4: T → a
            var grammar = new GrammarBuilder()
                .SetNonterminalSymbols(Symbol.Vs("S", "E", "T"))
                .SetTerminalSymbols(Symbol.Ts('a', '+', '(', ')'))
                .SetStartSymbol(Symbol.V("S"))
                .AndProductions(
                    Symbol.V("S").GoesTo(Symbol.V("E")),
                    Symbol.V("E").GoesTo(Symbol.V("E"), Symbol.T('+'), Symbol.V("T")),
                    Symbol.V("E").GoesTo(Symbol.V("T")),
                    Symbol.V("T").GoesTo(Symbol.T('('), Symbol.V("E"), Symbol.T(')')),
                    Symbol.V("T").GoesTo(Symbol.T('a'))
                );

            // Create NFA (digraph of items labeled by symbols)
            var characteristicStringsNfa = grammar.GetLr0AutomatonNfa();

            SaveFile("StanNCG.dot", DotLanguagePrinter.ToDotLanguage(characteristicStringsNfa, DotRankDirection.TopBottom));

            var dfa = characteristicStringsNfa.ToDfa();

            SaveFile("StanDCG.dot", DotLanguagePrinter.ToDotLanguage(dfa, DotRankDirection.LeftRight, skipStateLabeling:true));

            var dfa2 = grammar.GetLr0AutomatonDfa();

            SaveFile("StanDCGLr.dot", DotLanguagePrinter.ToDotLanguage(dfa2, DotRankDirection.LeftRight, skipStateLabeling:true));
        }

        public static void StanfordShiftReduceConflictGrammar()
        {
            // 0: S → E
            // 1: E → E + T
            // 2: E → T
            // 3: T → (E)
            // 4: T → a          (Dragon book has 'id' terminal here, but our model only supports single char tokens at the moment)
            // 5: T → a[E]       (Dragon book has 'id' terminal here, but our model only supports single char tokens at the moment)
            var grammar = new GrammarBuilder()
                .SetNonterminalSymbols(Symbol.Vs("S", "E", "T"))
                .SetTerminalSymbols(Symbol.Ts('a', '+', '(', ')', '[', ']'))
                .SetStartSymbol(Symbol.V("S"))
                .AndProductions(
                    Symbol.V("S").GoesTo(Symbol.V("E")),
                    Symbol.V("E").GoesTo(Symbol.V("E"), Symbol.T('+'), Symbol.V("T")),
                    Symbol.V("E").GoesTo(Symbol.V("T")),
                    Symbol.V("T").GoesTo(Symbol.T('('), Symbol.V("E"), Symbol.T(')')),
                    Symbol.V("T").GoesTo(Symbol.T('a')),
                    // Adding this rule we have a shift/reduce conflict {shift 7, reduce 4} on '[' in state 4,
                    // because state 4 contains the following core items {T → a•, T → a•[E]}
                    Symbol.V("T").GoesTo(Symbol.T('a'), Symbol.T('['), Symbol.V("E"), Symbol.T(']'))
                );

            var characteristicStringsNfa = grammar.GetLr0AutomatonNfa();

            SaveFile("StanShiftReduceConflictNCG.dot", DotLanguagePrinter.ToDotLanguage(characteristicStringsNfa, DotRankDirection.TopBottom));

            var dfa = characteristicStringsNfa.ToDfa();

            SaveFile("StanShiftReduceConflictDCG.dot", DotLanguagePrinter.ToDotLanguage(dfa, DotRankDirection.LeftRight, skipStateLabeling:true));

            var dfa2 = grammar.GetLr0AutomatonDfa();

            SaveFile("StanShiftReduceConflictDCGLr.dot", DotLanguagePrinter.ToDotLanguage(dfa2, DotRankDirection.LeftRight, skipStateLabeling:true));
        }

        public static void StanfordReduceReduceConflictGrammar()
        {
            // 0: S → E
            // 1: E → E + T
            // 2: E → T
            // 3: E → V = E
            // 4: T → (E)
            // 5: T → a          (Dragon book has 'id' terminal here, but our model only supports single char tokens at the moment)
            // 6: V → a          (Dragon book has 'id' terminal here, but our model only supports single char tokens at the moment)
            var grammar = new GrammarBuilder()
                .SetNonterminalSymbols(Symbol.Vs("S", "E", "T", "V"))
                .SetTerminalSymbols(Symbol.Ts('a', '+', '(', ')', '='))
                .SetStartSymbol(Symbol.V("S"))
                .AndProductions(
                    Symbol.V("S").GoesTo(Symbol.V("E")),
                    Symbol.V("E").GoesTo(Symbol.V("E"), Symbol.T('+'), Symbol.V("T")),
                    Symbol.V("E").GoesTo(Symbol.V("T")),
                    // Adding this rule we have a reduce/reduce conflict {reduce 5, reduce 6} in state 5 on every
                    // possible symbol (in LR(0) table), because state 5 contains the following core items {T → a•, V → a•}
                    Symbol.V("E").GoesTo(Symbol.V("V"), Symbol.T('='), Symbol.V("E")),
                    Symbol.V("T").GoesTo(Symbol.T('('), Symbol.V("E"), Symbol.T(')')),
                    Symbol.V("T").GoesTo(Symbol.T('a')),
                    Symbol.V("V").GoesTo(Symbol.T('a'))
                );

            var characteristicStringsNfa = grammar.GetLr0AutomatonNfa();

            SaveFile("StanReduceReduceConflictNCG.dot", DotLanguagePrinter.ToDotLanguage(characteristicStringsNfa, DotRankDirection.TopBottom));

            var dfa = characteristicStringsNfa.ToDfa();

            SaveFile("StanReduceReduceConflictDCG.dot", DotLanguagePrinter.ToDotLanguage(dfa, DotRankDirection.LeftRight, skipStateLabeling:true));

            var dfa2 = grammar.GetLr0AutomatonDfa();

            SaveFile("StanReduceReduceConflictDCGLr.dot", DotLanguagePrinter.ToDotLanguage(dfa2, DotRankDirection.LeftRight, skipStateLabeling:true));
        }

        public static void DanglingElseWhenParsing_iEtiEtSeS_ImpliesShiftReduceConflictAfterParsing_iEtiEtS_InState8()
        {
            // 0: S' → S$
            // 1: S → i E t S
            // 2: S → i E t S e S
            // 3: E → 0
            // 4: E → 1
            // where tokens i (if), t (then), e (else)
            var grammar = new GrammarBuilder()
                .SetNonterminalSymbols(Symbol.Vs("S'", "S", "E"))
                .SetTerminalSymbols(Symbol.Ts('i', 't', 'e', '0', '1'))
                .SetStartSymbol(Symbol.V("S'"))
                .AndProductions(
                    Symbol.V("S'").GoesTo(Symbol.V("S")),
                    Symbol.V("S").GoesTo(Symbol.T('i'), Symbol.V("E"), Symbol.T('t'), Symbol.V("S")),
                    Symbol.V("S").GoesTo(Symbol.T('i'), Symbol.V("E"), Symbol.T('t'), Symbol.V("S"), Symbol.T('e'),
                        Symbol.V("S")),
                    Symbol.V("E").GoesTo(Symbol.T('0')),
                    Symbol.V("E").GoesTo(Symbol.T('0'))
                );

            // Create NFA (digraph of items labeled by symbols)
            var characteristicStringsNfa = grammar.GetLr0AutomatonNfa();

            SaveFile("DanglingNCG.dot", DotLanguagePrinter.ToDotLanguage(characteristicStringsNfa, DotRankDirection.TopBottom));

            var dfa = characteristicStringsNfa.ToDfa();

            SaveFile("DanglingDCG.dot", DotLanguagePrinter.ToDotLanguage(dfa, DotRankDirection.LeftRight, skipStateLabeling:true));

            var dfa2 = grammar.GetLr0AutomatonDfa();

            SaveFile("DanglingDCGLr.dot", DotLanguagePrinter.ToDotLanguage(dfa2, DotRankDirection.LeftRight, skipStateLabeling:true));
        }

        //
        // Regular languages, NFA, DFA
        //

        public static void CourseExercise()
        {
            var dfa = new Dfa<string>('A', new [] {'B', 'E'});
            dfa.AddTrans('A', "0", 'E');
            dfa.AddTrans('A', "1", 'D');
            dfa.AddTrans('B', "0", 'A');
            dfa.AddTrans('B', "1", 'C');
            dfa.AddTrans('C', "0", 'G');
            dfa.AddTrans('C', "1", 'B');
            dfa.AddTrans('D', "0", 'E');
            dfa.AddTrans('D', "1", 'A');
            dfa.AddTrans('E', "0", 'H');
            dfa.AddTrans('E', "1", 'C');
            dfa.AddTrans('F', "0", 'C');
            dfa.AddTrans('F', "1", 'B');
            dfa.AddTrans('G', "0", 'F');
            dfa.AddTrans('G', "1", 'E');
            dfa.AddTrans('H', "0", 'B');
            dfa.AddTrans('H', "1", 'H');
            var minDfa = dfa.ToMinimumDfa();
            SaveFile("exercise.dot", DotLanguagePrinter.ToDotLanguage(minDfa));
        }

        public static void RegexParser()
        {
            //string re = "ab*";
            //string re = "(a+b)*";
            //string re = "bb";
            //string re = "(a+b)*ab";
            //string re = "((a+b)*ab)*";
            //string re = "((a+b)*ab)((a+b)*ab)";
            //string re = "(a+b)*abb";

            // L1: From slides on closure properties
            //string re = "((a+b)*a+ε)(bb)*b"; // ends in an odd number of b's

            // L2: From slides on closure properties
            //string re = "ε+(a+b)*b"; // ends in at least one 'b' and the empty string

            // TODO: Create L3 = L1 - L2 (via product DFA, where accepting state is any state where L1 accepts and L2 does not)

            // Are these equivalent
            //string re = "b*a(a+b)*";
            string re = "(a+b)*a(a+b)*";

            Regex regex = RegexTextbook.ParseRD(re);

            var dfa = regex.ToDfa(skipRenaming: true);

            SaveFile("regex.dot", DotLanguagePrinter.ToDotLanguage(dfa));
        }

        public static void KeywordAutomata()
        {
            //
            // Keyword search: Build NFA directly
            //

            // TODO: Vi antager, at alfabetet er de mulige ord i 'web' og 'ebay', da grafen ellers bliver meget uoverskuelig
            // NOTE: Grafen er allerede uoverskuelig pga de mange pile, da hver vertex kun kan have et input

            // 9,1,0 is part of every state, so we remove them from the naming strategy
            var nfaKeywords = new Nfa<string>(9, new [] {4, 8}, s => new Set<int>(new[] {0,1,9}).Contains(s) == false);
            nfaKeywords.AddTrans(Transition.EpsilonMove<string, int>(9, 1));
            nfaKeywords.AddTrans(Transition.EpsilonMove<string, int>(9, 0));
            // guessing is smart in NFA
            nfaKeywords.AddTrans(Transition.Move(9, "w", 9));
            nfaKeywords.AddTrans(Transition.Move(9, "e", 9));
            nfaKeywords.AddTrans(Transition.Move(9, "b", 9));
            nfaKeywords.AddTrans(Transition.Move(9, "a", 9));
            nfaKeywords.AddTrans(Transition.Move(9, "y", 9));
            // web
            nfaKeywords.AddTrans(Transition.Move(1, "w", 2));
            nfaKeywords.AddTrans(Transition.Move(2, "e", 3));
            nfaKeywords.AddTrans(Transition.Move(3, "b", 4));
            // ebay
            nfaKeywords.AddTrans(Transition.Move(0, "e", 5));
            nfaKeywords.AddTrans(Transition.Move(5, "b", 6));
            nfaKeywords.AddTrans(Transition.Move(6, "a", 7));
            nfaKeywords.AddTrans(Transition.Move(7, "y", 8));

            var dfaKeywords = nfaKeywords.ToDfa();

            // Den virker, men grafen er uoverskuelig da vi ikke kan placere noderne
            SaveFile("dfa_keywords.dot", DotLanguagePrinter.ToDotLanguage(dfaKeywords));

            Console.WriteLine("");

            foreach (var word in new[] { "goto", "web", "ebay", "webay", "web1" })
            {
                // NFA is tail whatever, that is webay is a match because the suffix ebay is matched
                Console.WriteLine($"dfaKeywords.Match({word}) = {dfaKeywords.IsMatch(word)}");
            }
        }

        public static void EquivalenceOfTwoDfas()
        {
            //
            // Equivalence of two DFAs (Example 4.21 in book)
            //
            var eqDfas = new Dfa<string>('A', new [] {'A', 'C', 'D'}); // start state is redundant for finding equivalent blocks
            // First DFA
            eqDfas.AddTrans('A', "0", 'A');
            eqDfas.AddTrans('A', "1", 'B');
            eqDfas.AddTrans('B', "0", 'A');
            eqDfas.AddTrans('B', "1", 'B');
            // Second DFA
            eqDfas.AddTrans('C', "0", 'D');
            eqDfas.AddTrans('C', "1", 'E');
            eqDfas.AddTrans('D', "0", 'D');
            eqDfas.AddTrans('D', "1", 'E');
            eqDfas.AddTrans('E', "0", 'C');
            eqDfas.AddTrans('E', "1", 'E');

            SaveFile("dfa_eq.dot", DotLanguagePrinter.ToDotLanguage(eqDfas));

            //System.Console.WriteLine();
            Console.WriteLine($"Eq state pairs: {eqDfas.DisplayEquivalentPairs()}");
            Console.WriteLine($"Eq state sets: {eqDfas.DisplayMergedEqSets()}");
        }

        public static void NonMinimalDfa()
        {
            //
            // Non-minimal DFA (Exercise 4.4.1 in the book)
            //
            var nonMinDfa = new Dfa<string>('A', new [] {'D'});
            nonMinDfa.AddTrans('A', "0", 'B');
            nonMinDfa.AddTrans('A', "1", 'A');
            nonMinDfa.AddTrans('B', "0", 'A');
            nonMinDfa.AddTrans('B', "1", 'C');
            nonMinDfa.AddTrans('C', "0", 'D');
            nonMinDfa.AddTrans('C', "1", 'B');
            nonMinDfa.AddTrans('D', "0", 'D');
            nonMinDfa.AddTrans('D', "1", 'A');
            nonMinDfa.AddTrans('E', "0", 'D');
            nonMinDfa.AddTrans('E', "1", 'F');
            nonMinDfa.AddTrans('F', "0", 'G');
            nonMinDfa.AddTrans('F', "1", 'E');
            nonMinDfa.AddTrans('G', "0", 'F');
            nonMinDfa.AddTrans('G', "1", 'G');
            nonMinDfa.AddTrans('H', "0", 'G');
            nonMinDfa.AddTrans('H', "1", 'D');

            SaveFile("dfaNonMin.dot", DotLanguagePrinter.ToDotLanguage(nonMinDfa));

            Console.WriteLine($"Eq state pairs: {nonMinDfa.DisplayEquivalentPairs()}");
            Console.WriteLine($"Eq state sets: {nonMinDfa.DisplayMergedEqSets()}");

            var minDfa = nonMinDfa.ToMinimumDfa();

            SaveFile("dfaMin.dot", DotLanguagePrinter.ToDotLanguage(minDfa));
        }

        public static void DecimalAutomata()
        {
            //
            // epsilon-NFA accepting accepting decimal numbers
            //

            var nfaDecimal = new Nfa<string>(0, 5);

            // TODO: Because we do not support ranges let d = [0-9]
            // TODO: Support characterRanges as spacial labels/inputs on transitions
            // TODO: Support putting single arc on every transition from p to q where label uses Sigma \ chars notation
            //            Sigma - {...}
            //            Sigma - d
            //            { .... }
            // TODO: Have the program calculate the label with fewest characters, and always use single arcs between any two nodes

            // sign
            nfaDecimal.AddTrans(Transition.EpsilonMove<string, int>(0, 1));
            nfaDecimal.AddTrans(Transition.Move(0, "+", 1));
            nfaDecimal.AddTrans(Transition.Move(0, "-", 1));
            // optional digits [0-9] before decimal point
            nfaDecimal.AddTrans(Transition.Move(1, "d", 1));
            //nfa.AddTrans(1, "1", 1);
            //nfa.AddTrans(1, "2", 1);
            //nfa.AddTrans(1, "3", 1);
            //nfa.AddTrans(1, "4", 1);
            //nfa.AddTrans(1, "5", 1);
            //nfa.AddTrans(1, "6", 1);
            //nfa.AddTrans(1, "7", 1);
            //nfa.AddTrans(1, "8", 1);
            //nfa.AddTrans(1, "9", 1);
            // decimal point before mandatory digit(s)
            nfaDecimal.AddTrans(Transition.Move(1, ".", 2));
            // digit after state 2
            nfaDecimal.AddTrans(Transition.Move(2, "d", 3));
            //nfa.AddTrans(2, "1", 3);
            //nfa.AddTrans(2, "2", 3);
            //nfa.AddTrans(2, "3", 3);
            //nfa.AddTrans(2, "4", 3);
            //nfa.AddTrans(2, "5", 3);
            //nfa.AddTrans(2, "6", 3);
            //nfa.AddTrans(2, "7", 3);
            //nfa.AddTrans(2, "8", 3);
            //nfa.AddTrans(2, "9", 3);
            // digit before decimal point
            nfaDecimal.AddTrans(Transition.Move(1, "d", 4));
            //nfa.AddTrans(1, "1", 4);
            //nfa.AddTrans(1, "2", 4);
            //nfa.AddTrans(1, "3", 4);
            //nfa.AddTrans(1, "4", 4);
            //nfa.AddTrans(1, "5", 4);
            //nfa.AddTrans(1, "6", 4);
            //nfa.AddTrans(1, "7", 4);
            //nfa.AddTrans(1, "8", 4);
            //nfa.AddTrans(1, "9", 4);
            // decimal point after mandatory digit(s)
            nfaDecimal.AddTrans(Transition.Move(4, ".", 3));
            // optional digits [0-9] after decimal point
            nfaDecimal.AddTrans(Transition.Move(3, "d", 3));
            //nfa.AddTrans(3, "1", 3);
            //nfa.AddTrans(3, "2", 3);
            //nfa.AddTrans(3, "3", 3);
            //nfa.AddTrans(3, "4", 3);
            //nfa.AddTrans(3, "5", 3);
            //nfa.AddTrans(3, "6", 3);
            //nfa.AddTrans(3, "7", 3);
            //nfa.AddTrans(3, "8", 3);
            //nfa.AddTrans(3, "9", 3);
            // epsilon-transition to accepting/final state
            nfaDecimal.AddTrans(Transition.EpsilonMove<string, int>(3, 5));

            var dfaDecimal = nfaDecimal.ToDfa();

            SaveFile("dfa_decimal.dot", DotLanguagePrinter.ToDotLanguage(dfaDecimal));

            foreach (var word in new[] { "+d.d", "-.", "-.d", ".", "d.", "d.d", ".d" })
            {
                Console.WriteLine($"dfaDecimal.Match({word}) = {dfaDecimal.IsMatch(word)}");
            }
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public static void OldMain()
        {
            //// SML reals: sign?((digit+(\.digit+)?))([eE]sign?digit+)?
            //Regex d = new Sym("digit");
            //Regex dPlus = new Seq(d, new Star(d));
            //Regex s = new Sym("sign");
            //Regex sOpt = new Alt(s, new Eps());
            //Regex dot = new Sym(".");
            //Regex dotDigOpt = new Alt(new Eps(), new Seq(dot, dPlus));
            //Regex mant = new Seq(sOpt, new Seq(dPlus, dotDigOpt));
            //Regex e = new Sym("e");
            //Regex exp = new Alt(new Eps(), new Seq(e, new Seq(sOpt, dPlus)));
            //Regex smlReal = new Seq(mant, exp);
            //BuildAndShow("dfa5.dot", smlReal);
        }

        private static void SaveFile(string filename, string contents)
        {
            File.WriteAllText(GetPath(filename), contents);
        }

        private static string GetPath(string filename)
        {
            string artifactsPath = GetArtifactsPath();
            Directory.CreateDirectory(artifactsPath);
            string path = Path.Combine(artifactsPath, filename);
            return path;
        }

        private static string GetArtifactsPath()
        {
            string path = new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath;
            while (true)
            {
                path = Path.GetDirectoryName(path);
                if (Directory.GetDirectories(path, ".git", SearchOption.TopDirectoryOnly).Length == 1)
                {
                    break;
                }
            }

            string artifactsPath = Path.Combine(path, "artifacts");
            return artifactsPath;
        }
    }

}
