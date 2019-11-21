using System.Collections.Generic;
using AutomataLib;

namespace ContextFreeGrammar.Analyzers
{
    public interface IErasableSymbolsAnalyzer
    {
        /// <summary>
        /// Can a given symbol (typically nonterminal) derive the empty string such that the nonterminal can be erased.
        /// </summary>
        /// <param name="symbol">The (typically nonterminal) symbol.</param>
        /// <returns>True, if the (typically nonterminal) is nullable (such that the nonterminal symbol is erasable).</returns>
        bool Erasable(Symbol symbol);

        // Nullable(X) = Nullable(Y1) Λ ... Λ Nullable(Yn), for X → Y1 Y2...Yn
        // ε-production has empty tail, but All/Or returns true for empty list
        bool Erasable(IEnumerable<Symbol> symbols);
    }

    public interface IFirstSymbolsAnalyzer<TTerminalSymbol>
    {
        /// <summary>
        /// The First function yields the set of starter symbols for a grammar symbol. It is formally
        /// defined as
        ///      FIRST(A) = { a ∈ T | A ∗⇒ aβ }
        /// for a nonterminal A ∈ N. We have extended the set-valued function to all grammar symbols.
        /// </summary>
        /// <param name="symbol">A single grammar symbol.</param>
        IReadOnlySet<TTerminalSymbol> First(Symbol symbol);

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
        /// <param name="symbols">The sequence of symbols (possibly empty, aka epsilon)</param>
        IReadOnlySet<TTerminalSymbol> First(IEnumerable<Symbol> symbols);

    }

    public interface IStarterSymbolsAnalyzer<TTerminalSymbol> : IErasableSymbolsAnalyzer, IFirstSymbolsAnalyzer<TTerminalSymbol>
    {
    }

    public interface IFollowSymbolsAnalyzer<in TNonterminalSymbol, TTerminalSymbol>
    {
        /// <summary>
        /// The FOLLOW function yields the set of symbols that may legally follow a grammar symbol in a
        /// sentential form. It is defined as
        ///      FOLLOW(A) = { a ∈ T| S ∗⇒ αAaβ }
        /// for a nonterminal A ∈ N.
        /// If there is a derivation of the form S ∗⇒ βA then $ (eof) is also added to FOLLOW(A). In
        /// particular, $ ∈ follow(S').
        /// </summary>
        /// <param name="variable"></param>
        /// <returns></returns>
        IReadOnlySet<TTerminalSymbol> Follow(TNonterminalSymbol variable);
    }
}
