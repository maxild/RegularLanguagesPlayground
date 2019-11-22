using System;
using System.Collections.Generic;
using System.Linq;
using AutomataLib;

namespace ContextFreeGrammar.Analyzers
{
    /// <summary>
    /// Nullable symbols analyzer.
    /// </summary>
    public interface IErasableSymbolsAnalyzer
    {
        /// <summary>
        /// Can a given symbol (typically nonterminal) derive the empty string such that the nonterminal can be erased.
        /// </summary>
        /// <param name="symbol">The (typically nonterminal) symbol.</param>
        /// <returns>True, if the (typically nonterminal) is nullable (such that the nonterminal symbol is erasable).</returns>
        /// <remarks>
        /// ERASABLE(eof) = ERASABLE(ε) = <c>true</c>
        /// </remarks>
        bool Erasable(Symbol symbol);
    }

    /// <summary>
    /// Starter tokens analyzer.
    /// </summary>
    public interface IFirstSetsAnalyzer<TTerminalSymbol>
        where TTerminalSymbol : IEquatable<TTerminalSymbol>
    {
        /// <summary>
        /// The First function yields the set of starter symbols for a grammar symbol. It is formally
        /// defined as
        ///      FIRST(A) = { a ∈ T | A ∗⇒ aβ }
        /// for a nonterminal A ∈ N. We have extended the set-valued function to all grammar symbols.
        /// </summary>
        /// <param name="symbol">A single grammar symbol.</param>
        /// <remarks>
        /// FIRST(eof) = {eof} = {$}
        /// FIRST(ε) = Ø
        /// </remarks>
        IReadOnlySet<TTerminalSymbol> First(Symbol symbol);
    }

    /// <summary>
    /// Combined 'nullable symbols' and 'starter tokens' analyzer.
    /// </summary>
    public interface IFirstSymbolsAnalyzer<TTerminalSymbol> : IErasableSymbolsAnalyzer, IFirstSetsAnalyzer<TTerminalSymbol>
        where TTerminalSymbol : IEquatable<TTerminalSymbol>
    {
    }

    /// <summary>
    /// Follower tokens analyzer.
    /// </summary>
    public interface IFollowSetsAnalyzer<in TNonterminalSymbol, TTerminalSymbol>
        where TTerminalSymbol : IEquatable<TTerminalSymbol>
    {
        /// <summary>
        /// The FOLLOW function yields the set of terminal symbols that may legally follow a nonterminal symbol in a
        /// sentential form. It is defined as
        ///      FOLLOW(A) = { a ∈ T| S ∗⇒ αAaβ }
        /// where A ∈ N.
        /// If there is a derivation of the form S ∗⇒ βA then $ (eof) is also added to FOLLOW(A). In
        /// particular, $ ∈ FOLLOW(S').
        /// </summary>
        /// <param name="variable">A nonterminal symbol.</param>
        /// <returns>The set of terminal symbols that may legally follow a nonterminal symbol.</returns>
        IReadOnlySet<TTerminalSymbol> Follow(TNonterminalSymbol variable);
    }

    /// <summary>
    /// Combined 'nullable symbols', 'starter tokens' and 'follower tokens' analyzer.
    /// </summary>
    public interface IFollowSymbolsAnalyzer<in TNonterminalSymbol, TTerminalSymbol> :
        IFirstSymbolsAnalyzer<TTerminalSymbol>, IFollowSetsAnalyzer<TNonterminalSymbol, TTerminalSymbol>
        where TTerminalSymbol : IEquatable<TTerminalSymbol>
    {
    }

    /// <summary>
    /// Extend Erasable, First and Follow to the free Monoid of symbol sequences.
    /// </summary>
    public static class SymbolsAnalyzerExtensions
    {
        public static bool Erasable(this IErasableSymbolsAnalyzer analyzer, IEnumerable<Symbol> symbols)
        {
            // Nullable(X) = Nullable(Y1) Λ ... Λ Nullable(Yn), for X → Y1 Y2...Yn
            // Nullable(ε) = false
            return symbols.All(analyzer.Erasable); // All (&&-Monoid) is false by default
        }

        // TODO: Insert into symbols overload
        // FIRST can be thought of as the extension of START, but often FIRST is defined for both single symbols
        // and sentential forms. That is FIRST is extended to all grammar symbols (i.e. sentential forms)
        // The FIRST function is a simple extension of START (single symbol) to the domain of sentential forms.
        //      FIRST(α) = { x ∈ T | α ∗⇒ xβ }
        // An alternative definition which shows how to derive FIRST from START recursively is
        //      FIRST(X1X2...Xk) = START(X1) ∪ FIRST(X2...Xk), if X1 is nullable
        //      FIRST(X1X2...Xk) = START(X1)                    otherwise
        //      FIRST(ε) = Ø = { }

        /// <summary>
        /// The First function yields the set of starter symbols for a sequence of grammar symbols. It is formally
        /// defined as
        ///      FIRST(α) = { a ∈ T | α ∗⇒ aβ }
        /// for any sentential form α ∈ (T ∪ N)*. We have therefore extended the set-valued function to all sentential forms.
        /// </summary>
        /// <param name="analyzer"></param>
        /// <param name="symbols">The sequence of symbols (possibly empty, aka epsilon)</param>
        public static IReadOnlySet<TTerminalSymbol> First<TTerminalSymbol>(
            this IFirstSymbolsAnalyzer<TTerminalSymbol> analyzer,
            IEnumerable<Symbol> symbols) where TTerminalSymbol : IEquatable<TTerminalSymbol>
        {
            var first = new Set<TTerminalSymbol>();
            foreach (var symbol in symbols)
            {
                first.AddRange(analyzer.First(symbol));
                if (!analyzer.Erasable(symbol)) break;
            }
            return first;
        }
    }
}
