using System;
using System.Collections.Generic;
using System.Linq;

namespace AutomataLib
{
    public abstract class Letterizer<TAlphabet>
    {
        public abstract IEnumerable<TAlphabet> GetLetters(string s);

        public static Letterizer<TAlphabet> Default { get; } = CreateLetterizer();

        private static Letterizer<TAlphabet> CreateLetterizer()
        {
            Type t = typeof(TAlphabet);
            if (t == typeof(char))
            {
                return (Letterizer<TAlphabet>)(object)(new CharLetterizer());
            }
            if (t == typeof(string))
            {
                return (Letterizer<TAlphabet>)(object)(new StringLetterizer());
            }
            throw new NotSupportedException($"A letterizer for the type '{t.Name}' is not supported.");
        }

        class StringLetterizer : Letterizer<string>
        {
            public override IEnumerable<string> GetLetters(string s)
            {
                return s.Select(c => new string(c, 1));
            }
        }

        class CharLetterizer : Letterizer<char>
        {
            public override IEnumerable<char> GetLetters(string s)
            {
                return s;
            }
        }
    }
}
