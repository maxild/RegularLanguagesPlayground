using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using AutomataLib;
using AutomataLib.Graphs;
using CLI.TestDriver.Parsers;
using ContextFreeGrammar;
using ContextFreeGrammar.Analyzers;
using FiniteAutomata;
using GrammarRepo;

namespace CLI.TestDriver
{
    static class Program
    {
        public static void Main()
        {
            //GallierLookaheadLR_Example3();
            //DigraphMethods();

            //DragonBookEx4_54();
            DragonBookEx4_48();
            //StanfordShiftReduceConflictGrammar();
            //StanfordReduceReduceConflictGrammar();
            //ExprGrammarStanfordNotesOnBottomUpParsing();
            //DanglingElseWhenParsing_iEtiEtSeS_ImpliesShiftReduceConflictAfterParsing_iEtiEtS_InState8();
            //ExprGrammarCh4DragonBook();
            //GallierToyExampleLr0();
            //GallierToyExampleLr1();
            //GallierExprGrammarLr0();
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

        /// <summary>
        /// G3 in "A Survey of LR-Parsing Methods", Gallier.
        /// </summary>
        public static void GallierLookaheadLR_Example3()
        {
            var grammar = DragonBookExample4_48.GetGrammar();

            // characteristic automaton (LR(0) automaton)
            var dfaLr0 = grammar.GetLr0AutomatonDfa();

            SaveFile("GallierEx3_LR0Automaton.dot", DotLanguagePrinter.ToDotLanguage(dfaLr0, DotRankDirection.LeftRight, skipStateLabeling: true));

            var analyzer = Analyzers.CreateErasableSymbolsAnalyzer(grammar);

            //(ImmutableArray<IReadOnlySet<Terminal>> initfirstSets, IGraph graphFirst) = DigraphAlgorithm.GetFirstGraph(grammar, analyzer);
            (var initfirstSets, IGraph graphFirst) = DigraphAlgorithm.GetFirstGraph(grammar, analyzer);

            SaveFile("GallierEx3_FirstGraph.dot",
                DotLanguagePrinter.PrintGraph("INITFIRST", initfirstSets, graphFirst, v => grammar.Variables[v].Name));

            var firstSymbolsAnalyzer = Analyzers.CreateFirstSymbolsAnalyzer(grammar);

            (var initFollowSets, IGraph graphFollow) = DigraphAlgorithm.GetFollowGraph(grammar, firstSymbolsAnalyzer);

            SaveFile("GallierEx3_FollowGraph.dot",
                DotLanguagePrinter.PrintGraph("INITFOLLOW", initFollowSets, graphFollow, v => grammar.Variables[v].Name));

            var stringWriter = new StringWriter();
            grammar.PrintFirstAndFollowSets(stringWriter);
            SaveFile("GallierEx3_FirstAndFollowSets.txt", stringWriter.ToString());

            // Grammar is LR(0)
            var lr0Parser = grammar.ComputeLr0ParsingTable();
            var writer = new StringWriter();
            lr0Parser.PrintParsingTable(writer);

            foreach (var conflict in lr0Parser.Conflicts)
            {
                writer.WriteLine(conflict);
                writer.WriteLine($"In state {conflict.State}: {lr0Parser.GetItems(conflict.State).KernelItems.ToVectorString()} (kernel items)");
            }
            writer.WriteLine();

            SaveFile("GallierEx3_Lr0ParsingTable.txt", writer.ToString());

            ////

            var vertices = LalrLookaheadSetsAlgorithm.GetGotoTransitionPairs(grammar, dfaLr0);

            // Read (INITFOLLOW) sets
            var (directReads, graphRead) = LalrLookaheadSetsAlgorithm.GetGraphReads(grammar, dfaLr0, vertices, analyzer);

            SaveFile("GallierEx3_ReadGraph.dot",
                DotLanguagePrinter.PrintGraph("DR", directReads, graphRead, v => vertices[v].ToString()));

            var graphLaFollow = LalrLookaheadSetsAlgorithm.GetGraphLaFollow(grammar, dfaLr0, vertices, analyzer);

            SaveFile("GallierEx3_LaFollowGraph.dot",
                DotLanguagePrinter.PrintGraph("INITFOLLOW", directReads, graphLaFollow, v => vertices[v].ToString()));

        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public static void DigraphMethods()
        {
            var grammar = GallierCalc.GetGrammar();

            var analyzer = Analyzers.CreateErasableSymbolsAnalyzer(grammar);

            (var initfirstSets, IGraph graphFirst) = DigraphAlgorithm.GetFirstGraph(grammar, analyzer);

            SaveFile("FirstGraph.dot", DotLanguagePrinter.PrintGraph("INITFIRST", initfirstSets, graphFirst, v => grammar.Variables[v].Name));

            var firstSymbolsAnalyzer = Analyzers.CreateFirstSymbolsAnalyzer(grammar);

            (var initFollowSets, IGraph graphFollow) = DigraphAlgorithm.GetFollowGraph(grammar, firstSymbolsAnalyzer);

            SaveFile("FollowGraph.dot", DotLanguagePrinter.PrintGraph("INITFOLLOW", initFollowSets, graphFollow, v => grammar.Variables[v].Name));
        }

        public static void GrammarSomething()
        {
            // See also http://www.lsv.fr/Publis/PAPERS/PDF/schmitz-dlt06.pdf
            // The following grammar generates the regular language: aa+ | aa∗b.
            // 0: S' → S
            // 1: S → BC
            // 2: S → AD
            // 3: A → a
            // 4: B → a
            // 5: C → CA
            // 6: C → A
            // 7: D → aD
            // 8: D → b
            // TODO: Create this example some day
        }

        public static void DragonBookEx4_54()
        {
            var grammar = DragonBookExample4_54.GetGrammar();

            var dfaLr0 = grammar.GetLr0AutomatonDfa();

            SaveFile("DragonBookEx4_54_DCG0Lr.dot", DotLanguagePrinter.ToDotLanguage(dfaLr0, DotRankDirection.LeftRight, skipStateLabeling: true));

            var dfaLr1 = grammar.GetLr1AutomatonDfa();

            SaveFile("DragonBookEx4_54_DCG1Lr.dot", DotLanguagePrinter.ToDotLanguage(dfaLr1, DotRankDirection.LeftRight, skipStateLabeling: true));
        }

        /// <summary>
        /// G3 in "A Survey of LR-Parsing Methods", Gallier.
        /// </summary>
        public static void GallierToyExampleLr1()
        {
            var grammar = GallierG3.GetGrammar();

            var characteristicStringsNfa = grammar.GetLr1AutomatonNfa();

            SaveFile("GallierNCG1.dot", DotLanguagePrinter.ToDotLanguage(characteristicStringsNfa, DotRankDirection.TopBottom));

            var dfa = characteristicStringsNfa.ToDfa();

            SaveFile("GallierDCG1.dot", DotLanguagePrinter.ToDotLanguage(dfa, DotRankDirection.LeftRight, skipStateLabeling: true));

            var dfa2 = grammar.GetLr1AutomatonDfa();

            SaveFile("GallierDCG1Lr.dot", DotLanguagePrinter.ToDotLanguage(dfa2, DotRankDirection.LeftRight, skipStateLabeling: true));

            // TODO: Parsing table in unit test
        }

        /// <summary>
        /// G1 in "A Survey of LR-Parsing Methods", Gallier.
        /// </summary>
        public static void GallierToyExampleLr0()
        {
            var grammar = GallierG1.GetGrammar();

            // Create NFA (digraph of items labeled by symbols)
            var characteristicStringsNfa = grammar.GetLr0AutomatonNfa();

            SaveFile("GallierNCG0.dot", DotLanguagePrinter.ToDotLanguage(characteristicStringsNfa, DotRankDirection.TopBottom));

            var dfa = characteristicStringsNfa.ToDfa();

            SaveFile("GallierDCG0.dot", DotLanguagePrinter.ToDotLanguage(dfa, DotRankDirection.LeftRight, skipStateLabeling: true));

            var dfa2 = grammar.GetLr0AutomatonDfa();

            SaveFile("GallierDCG0Lr.dot", DotLanguagePrinter.ToDotLanguage(dfa2, DotRankDirection.LeftRight, skipStateLabeling: true));
        }

        public static void ExprGrammarCh4DragonBook()
        {
            var grammar = DragonBook_ExprGrammarCh4.GetGrammar();

            // Create NFA (digraph of items labeled by symbols)
            var characteristicStringsNfa = grammar.GetLr0AutomatonNfa();

            SaveFile("DragonNCG.dot", DotLanguagePrinter.ToDotLanguage(characteristicStringsNfa, DotRankDirection.TopBottom));

            var dfa = characteristicStringsNfa.ToDfa();

            SaveFile("DragonDCG.dot", DotLanguagePrinter.ToDotLanguage(dfa, DotRankDirection.LeftRight, skipStateLabeling:true));

            var dfa2 = grammar.GetLr0AutomatonDfa();

            SaveFile("DragonDCGLr.dot", DotLanguagePrinter.ToDotLanguage(dfa2, DotRankDirection.LeftRight, skipStateLabeling:true));

            var lexer = DragonBook_ExprGrammarCh4.GetLexer("a*a+a");

            grammar.ComputeSlrParsingTable().Parse(lexer, Console.Out);
        }

        /// <summary>
        /// Example 4.48 in Dragon book p. 255, 2nd ed
        /// </summary>
        public static void DragonBookEx4_48()
        {
            var grammar = DragonBookExample4_48.GetGrammar();

            //
            // LR(0) automaton
            //

            var nfa0 = grammar.GetLr0AutomatonNfa();

            SaveFile("DragonBookEx4_48_NCG0.dot", DotLanguagePrinter.ToDotLanguage(nfa0, DotRankDirection.TopBottom));

            var dfa0 = nfa0.ToDfa();

            SaveFile("DragonBookEx4_48_DCG0.dot", DotLanguagePrinter.ToDotLanguage(dfa0, DotRankDirection.LeftRight, skipStateLabeling:true));

            var dfaLr0 = grammar.GetLr0AutomatonDfa();

            SaveFile("DragonBookEx4_48_DCG0Lr.dot", DotLanguagePrinter.ToDotLanguage(dfaLr0, DotRankDirection.LeftRight, skipStateLabeling:true));

            // We will augment every LR(0) item with information about what portion of the follow set is appropriate given
            // the path we have taken to that state. We can be in state 2 {S → L•=R, R → L•} for one of two reasons
            //     (i)  We are trying to build from S => L=R    (shift =)
            //     (ii) We are trying to build from S => R => L (reduce by R → L•)

            //
            // LR(1) automaton
            //

            var nfa1 = grammar.GetLr1AutomatonNfa();

            SaveFile("DragonBookEx4_48_NCG1.dot", DotLanguagePrinter.ToDotLanguage(nfa1, DotRankDirection.TopBottom));

            var dfa1 = nfa1.ToDfa();

            SaveFile("DragonBookEx4_48_DCG1.dot", DotLanguagePrinter.ToDotLanguage(dfa1, DotRankDirection.LeftRight, skipStateLabeling:true));

            var dfaLr1 = grammar.GetLr1AutomatonDfa();

            SaveFile("DragonBookEx4_48_DCG1Lr.dot", DotLanguagePrinter.ToDotLanguage(dfaLr1, DotRankDirection.LeftRight, skipStateLabeling:true));

            // TODO: Parse 'a=a'
        }

        public static void StanfordShiftReduceConflictGrammar()
        {
            var grammar = StanfordShiftReduceConflict.GetGrammar();

            var characteristicStringsNfa = grammar.GetLr0AutomatonNfa();

            SaveFile("StanShiftReduceConflictNCG.dot", DotLanguagePrinter.ToDotLanguage(characteristicStringsNfa, DotRankDirection.TopBottom));

            var dfa = characteristicStringsNfa.ToDfa();

            SaveFile("StanShiftReduceConflictDCG.dot", DotLanguagePrinter.ToDotLanguage(dfa, DotRankDirection.LeftRight, skipStateLabeling:true));

            var dfa2 = grammar.GetLr0AutomatonDfa();

            SaveFile("StanShiftReduceConflictDCGLr.dot", DotLanguagePrinter.ToDotLanguage(dfa2, DotRankDirection.LeftRight, skipStateLabeling:true));
        }

        public static void StanfordReduceReduceConflictGrammar()
        {
            var grammar = StanfordReduceReduceConflict.GetGrammar();

            var characteristicStringsNfa = grammar.GetLr0AutomatonNfa();

            SaveFile("StanReduceReduceConflictNCG.dot", DotLanguagePrinter.ToDotLanguage(characteristicStringsNfa, DotRankDirection.TopBottom));

            var dfa = characteristicStringsNfa.ToDfa();

            SaveFile("StanReduceReduceConflictDCG.dot", DotLanguagePrinter.ToDotLanguage(dfa, DotRankDirection.LeftRight, skipStateLabeling:true));

            var dfa2 = grammar.GetLr0AutomatonDfa();

            SaveFile("StanReduceReduceConflictDCGLr.dot", DotLanguagePrinter.ToDotLanguage(dfa2, DotRankDirection.LeftRight, skipStateLabeling:true));
        }

        public static void DanglingElseWhenParsing_iEtiEtSeS_ImpliesShiftReduceConflictAfterParsing_iEtiEtS_InState8()
        {
            var grammar = DanglingElse.GetGrammar();

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
                var letters = Letterizer<string>.Default.GetLetters(word);
                // NFA is tail whatever, that is webay is a match because the suffix ebay is matched
                Console.WriteLine($"dfaKeywords.Match({word}) = {dfaKeywords.IsMatch(letters)}");
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
                var letters = Letterizer<string>.Default.GetLetters(word);
                Console.WriteLine($"dfaDecimal.Match({word}) = {dfaDecimal.IsMatch(letters)}");
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
