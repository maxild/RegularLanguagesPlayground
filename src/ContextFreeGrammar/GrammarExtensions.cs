using System;
using System.Collections.Generic;
using System.Linq;
using AutomataLib;

namespace ContextFreeGrammar
{
    // TODO: make them available on Grammar...maybe drop Terminals (T), Nonterminals (V)...V is odd...use N
    // TODO: Better to use aliases and also have the long words
    public static class GrammarExtensions
    {
        /// <summary>
        /// Reserved (extended) terminal symbol for end of input ('$' in dragon book).
        /// </summary>
        /// <remarks>
        /// Many texts on parsing and compiler theory will not agree that the eof marker ($) is a terminal symbol.
        /// In a way this is correct, because the language (per se) cannot contain this token. But in a way 'end
        /// of input' must be communicated from the lexer to the parser some way, and the most elegant (pure)
        /// way, is to extend the input alphabet T with this reserved token: T' = T ∪ {$}.
        /// Any valid grammar will only contain a single production containing the eof marker. This special
        /// production rule is by convention the first production of the grammar. This production
        /// S' → S$ give rise to two kernel items [S' → •S$], the initial item (state 1 in our implementation), and [S' → S•$], the final
        /// accepting item (state 2 in our implementation). This way the special S' → S$ rule is added to the grammar to allow
        /// the parser to accept the input in a deterministic way. That is a bottom-up (left) parser will only accept the input
        /// if the next input token is eof ($) after reducing by the final accept item [S' → S•$].
        /// </remarks>
        public static Terminal<TTokenKind> Eof<TTokenKind, TNonterminal>(this Grammar<TTokenKind, TNonterminal> grammar)
            where TTokenKind : struct, Enum
            where TNonterminal : struct, Enum
        {
            // TODO: At index zero, because of enforced convention
            return grammar.Terminals[TokenKinds.Eof];
        }

        public static Terminal<TTokenKind> T<TTokenKind, TNonterminal>(
            this Grammar<TTokenKind, TNonterminal> grammar,
            TTokenKind index
        )
            where TTokenKind : struct, Enum
            where TNonterminal : struct, Enum
        {
            return grammar.Terminals[index];
        }

        public static IReadOnlyList<Terminal<TTokenKind>> Ts<TTokenKind, TNonterminal>(
            this Grammar<TTokenKind, TNonterminal> grammar,
            params TTokenKind[] tokenKinds
            )
            where TTokenKind : struct, Enum
            where TNonterminal : struct, Enum
        {
            return tokenKinds.Select(kind => grammar.Terminals[kind]).ToArray();
        }

        public static Nonterminal V<TTokenKind, TNonterminal>(
            this Grammar<TTokenKind, TNonterminal> grammar,
            TNonterminal index
        )
            where TTokenKind : struct, Enum
            where TNonterminal : struct, Enum
        {
            return grammar.Nonterminals[index];
        }

        public static IReadOnlyList<Nonterminal> Vs<TTokenKind, TNonterminal>(
            this Grammar<TTokenKind, TNonterminal> grammar,
            params TNonterminal[] indices
        )
            where TTokenKind : struct, Enum
            where TNonterminal : struct, Enum
        {
            return indices.Select(index => grammar.Nonterminals[index]).ToArray();
        }
    }
}
