using System;
using System.Collections.Generic;
using AutomataLib;
using ContextFreeGrammar.Analyzers;

namespace ContextFreeGrammar
{
    /// <summary>
    /// G = (N, T, P, S) is a tuple, where
    /// N is a finite set of _nonterminal symbols_ (that is disjoint with the strings formed from G),
    /// T is a finite set of _terminal symbols_ (that is disjoint from N)
    /// P is a finite set of _production rules_, each rule is a relation-pair (head, tail) in (N, (N U T)* written head -> tail
    /// A distinguished symbol S in N that is the start symbol.
    /// </summary>
    public class GrammarBuilder
    {
        public GrammarBuilder<TTokenKind> Terminals<TTokenKind>() where TTokenKind : struct, Enum
        {
            return new GrammarBuilder<TTokenKind>();
        }
    }

    public class GrammarBuilder<TTokenKind> where TTokenKind : struct, Enum
    {
        public GrammarBuilder<TTokenKind, TNonterminal> Nonterminals<TNonterminal>() where TNonterminal : struct, Enum
        {
            return new GrammarBuilder<TTokenKind, TNonterminal>();
        }
    }

    public class GrammarBuilder<TTokenKind, TNonterminal>
        where TTokenKind : struct, Enum
        where TNonterminal : struct, Enum
    {
        private readonly SymbolCache<TTokenKind, Terminal<TTokenKind>> _terminals;
        private readonly SymbolCache<TNonterminal, Nonterminal> _nonterminals;
        private Nonterminal _startSymbol;
        private Func<Grammar<TTokenKind, TNonterminal>, IFollowSymbolsAnalyzer<TTokenKind>> _analyzerFactory;

        public GrammarBuilder()
        {
            var terminals = EnumUtils.MapToSymbolCache<TTokenKind, Terminal<TTokenKind>>((name, index, kind) =>
                new Terminal<TTokenKind>(name, index, kind));

            if (terminals.Count == 0)
                throw new InvalidOperationException("Grammar cannot have empty set of terminals.");
            if (!terminals.Contains(TokenKinds.Eof))
                throw new InvalidOperationException("EOF must be defined.");
            // We don't need this, but I like the convention
            if (!terminals[0].Name.Equals(TokenKinds.Eof, StringComparison.Ordinal))
                throw new InvalidOperationException("EOF must have index zero.");
            // TODO: validate that all terminals are uppercase words: NUM, ID etc... (convention)

            var nonterminals =
                EnumUtils.MapToSymbolCache<TNonterminal, Nonterminal>((name, index, _) =>
                    new Nonterminal(name, index, typeof(TNonterminal)));

            if (nonterminals.Count == 0)
                throw new InvalidOperationException("Grammar cannot have empty set of nonterminals.");
            // TODO: nonterminals are single upper case letter, or, lowercase word: expression, term, factor, declaration (convention)

            _terminals = terminals;
            _nonterminals = nonterminals;
        }

        public GrammarBuilder<TTokenKind, TNonterminal> SetAnalyzer(
            Func<Grammar<TTokenKind, TNonterminal>, IFollowSymbolsAnalyzer<TTokenKind>> analyzerFactory)
        {
            _analyzerFactory = analyzerFactory;
            return this;
        }

        public GrammarBuilder<TTokenKind, TNonterminal> StartSymbol(TNonterminal startIndex)
        {
            _startSymbol = _nonterminals[startIndex];
            return this;
        }

        public Grammar<TTokenKind, TNonterminal> And(Func<ProductionsBuilder, IReadOnlyList<Production>> fn)
        {
            var productions = fn(new ProductionsBuilder(this));

            return new Grammar<TTokenKind, TNonterminal>(_terminals, _nonterminals, _startSymbol, productions,
                _analyzerFactory ?? (grammar => new DragonBookAnalyzer<TTokenKind, TNonterminal>(grammar)));
        }

        public class ProductionsBuilder
        {
            private readonly GrammarBuilder<TTokenKind, TNonterminal> _builder;

            public ProductionsBuilder(GrammarBuilder<TTokenKind, TNonterminal> builder)
            {
                _builder = builder;
            }

            public IReadOnlyList<Production> Rules(params Production[] productions)
            {
                return productions;
            }

            public Nonterminal this[TNonterminal index] => _builder._nonterminals[index];

            public Terminal<TTokenKind> this[TTokenKind index] => _builder._terminals[index];
        }
    }

    //public class GrammarBuilder<TTokenKind> where TTokenKind : struct, Enum
    //{
    //    private IEnumerable<Nonterminal> _nonterminals;
    //    private readonly IReadOnlyList<Terminal<TTokenKind>> _terminals;
    //    private Nonterminal _startSymbol;
    //    private Func<Grammar<TTokenKind>, IFollowSymbolsAnalyzer<TTokenKind>> _analyzerFactory;

    //    public GrammarBuilder()
    //    {
    //        _terminals = Terminal<TTokenKind>.All;
    //    }

    //    public GrammarBuilder<TTokenKind> SetStartSymbol(Nonterminal start)
    //    {
    //        _startSymbol = start;
    //        return this;
    //    }

    //    public GrammarBuilder<TTokenKind> SetNonterminalSymbols(IEnumerable<Nonterminal> nonterminals)
    //    {
    //        _nonterminals = nonterminals ?? Enumerable.Empty<Nonterminal>();
    //        return this;
    //    }

    //    public GrammarBuilder<TTokenKind> SetAnalyzer(
    //        Func<Grammar<TTokenKind>, IFollowSymbolsAnalyzer<TTokenKind>> analyzerFactory)
    //    {
    //        _analyzerFactory = analyzerFactory;
    //        return this;
    //    }

    //    public Grammar<TTokenKind> AndProductions(params Production[] productions)
    //    {
    //        return new Grammar<TTokenKind>(_nonterminals, _terminals, _startSymbol, productions,
    //            _analyzerFactory ?? (grammar => new DragonBookAnalyzer<TTokenKind>(grammar)));
    //    }
    //}
}
