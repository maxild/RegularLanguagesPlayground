using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using AutomataLib;
using AutomataRepresentations;
using Shouldly;
using Xunit;

namespace UnitTests
{
    public class AutomataTests
    {
        [Fact]
        public void Trie()
        {
            // Language = {"ace$", "add$", "bad$" "bed$", "bee$", "cab$", "dad$"}
            var dfaTrie = new DfaAdjacencyMatrix<int>(
                Enumerable.Range(0, 20),            // 0..19
                SetOf('a', 'b', 'c', 'd', 'e','$'), // $ is EOF
                new[]
                {
                    // state 0 is error state (could be unreachable, if DFA accept any input string???)
                    Transition.Move(1, 'a', 2),
                    Transition.Move(1, 'b', 3),
                    Transition.Move(1, 'c', 4),
                    Transition.Move(1, 'd', 5),
                    Transition.Move(2, 'c', 16),
                    Transition.Move(2, 'd', 17),
                    Transition.Move(3, 'a', 11),
                    Transition.Move(3, 'e', 12),
                    Transition.Move(4, 'a', 9),
                    Transition.Move(5, 'a', 6),
                    Transition.Move(6, 'd', 7),
                    Transition.Move(7, '$', 8),
                    // state 8 is accept state
                    Transition.Move(9, 'b', 10),
                    Transition.Move(10, '$', 8),
                    Transition.Move(11, 'd', 15),
                    Transition.Move(12, 'd', 13),
                    Transition.Move(12, 'e', 14),
                    Transition.Move(13, '$', 8),
                    Transition.Move(14, '$', 8),
                    Transition.Move(15, '$', 8),
                    Transition.Move(16, 'e', 19),
                    Transition.Move(17, 'd', 18),
                    Transition.Move(18, '$', 8),
                    Transition.Move(19, '$', 8),
                },
                startState: 1,
                acceptStates: SetOf(8));

            // sparse transition table that would benefit from compression
            dfaTrie.AlphabetSize.ShouldBe(101 - 36 + 1);
            dfaTrie.StateSize.ShouldBe(20);
            dfaTrie.TableSize.ShouldBe(1320); // 20 * 66

            // Only 24 out of 1320 cells in the transition table are actually used to simulate the DFA
            dfaTrie.GetTrimmedTableSize().ShouldBe(24);

            // http://viz-js.com/
            SaveFile("trie.dot", DotLanguagePrinter.ToDotLanguage(dfaTrie));
        }

        [Fact]
        public void LexerWithKeywords()
        {
            var dfaTrie = new DfaAdjacencyMatrix<int>(
                Enumerable.Range(0, 8), // 0..7
                SetOf('e', 'n', 'd', 'l', 's'),
                new[]
                {
                    // state 0 is error state
                    Transition.Move(1, 'e', 2),
                    Transition.Move(2, 'n', 3),
                    Transition.Move(2, 'l', 3),
                    Transition.Move(3, 'd', 4),
                    Transition.Move(5, 's', 6),
                    Transition.Move(6, 'e', 7),
                },
                startState: 1,
                acceptStates: SetOf(7));

            // sparse transition table that would benefit from compression
            //dfaTrie.AlphabetSize.ShouldBe(101 - 36 + 1);
            //dfaTrie.StateSize.ShouldBe(20);
            //dfaTrie.TableSize.ShouldBe(1320); // 20 * 66

            SaveFile("keywords.dot", DotLanguagePrinter.ToDotLanguage(dfaTrie));
        }

        private IEnumerable<T> SetOf<T>(params T[] set)
        {
            return set;
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
