using System.Diagnostics.CodeAnalysis;
using ContextFreeGrammar;

namespace GrammarRepo
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static class DanglingElse
    {
        public enum Sym
        {
            IF,
            THEN,
            ELSE,
            TRUE,
            FALSE,
            EOF
        }

        public static Grammar<Sym> GetGrammar()
        {
            // 0: S' → S$
            // 1: S → i E t S
            // 2: S → i E t S e S
            // 3: E → 0
            // 4: E → 1
            // where tokens i (if), t (then), e (else)
            var grammar = new GrammarBuilder<Sym>()
                .SetNonterminalSymbols(Symbol.Vs("S'", "S", "E"))
                //.SetTerminalSymbols(Symbol.Ts('i', 't', 'e', '0', '1'))
                .SetStartSymbol(Symbol.V("S'"))
                .AndProductions(
                    Symbol.V("S'").Derives(Symbol.V("S")),
                    Symbol.V("S").Derives(Symbol.T(Sym.IF), Symbol.V("E"), Symbol.T(Sym.THEN), Symbol.V("S")),
                    Symbol.V("S").Derives(Symbol.T(Sym.IF), Symbol.V("E"), Symbol.T(Sym.THEN), Symbol.V("S"), Symbol.T(Sym.ELSE), Symbol.V("S")),
                    Symbol.V("E").Derives(Symbol.T(Sym.TRUE)),
                    Symbol.V("E").Derives(Symbol.T(Sym.FALSE))
                );
            return grammar;
        }
    }
}
