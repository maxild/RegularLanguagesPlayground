using System.Diagnostics.CodeAnalysis;
using AutomataLib;
using ContextFreeGrammar;

namespace GrammarRepo
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static class DanglingElse
    {
        public enum Sym
        {
            EPS = 0,
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


    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static class StanfordShiftReduceConflict
    {
        public enum Sym
        {
            EPS = 0,
            PLUS,       // +
            LPARAN,     // (
            RPARAN,     // )
            LBRACKET,   // [
            RBRACKET,   // ]
            ID,         // hardcoded to identifier a in notes
            EOF
        }

        public static Grammar<Sym> GetGrammar()
        {
            // 0: S → E
            // 1: E → E + T
            // 2: E → T
            // 3: T → (E)
            // 4: T → ID
            // 5: T → ID[E]
            var grammar = new GrammarBuilder<Sym>()
                .SetNonterminalSymbols(Symbol.Vs("S", "E", "T"))
                //.SetTerminalSymbols(Symbol.Ts('a', '+', '(', ')', '[', ']'))
                .SetStartSymbol(Symbol.V("S"))
                .AndProductions(
                    Symbol.V("S").Derives(Symbol.V("E")),
                    Symbol.V("E").Derives(Symbol.V("E"), Symbol.T(Sym.PLUS), Symbol.V("T")),
                    Symbol.V("E").Derives(Symbol.V("T")),
                    Symbol.V("T").Derives(Symbol.T(Sym.LPARAN), Symbol.V("E"), Symbol.T(Sym.RPARAN)),
                    Symbol.V("T").Derives(Symbol.T(Sym.ID)),
                    // Adding this rule we have a shift/reduce conflict {shift 7, reduce 4} on '[' in state 4,
                    // because state 4 contains the following kernel items {T → a•, T → a•[E]}
                    Symbol.V("T").Derives(Symbol.T(Sym.ID), Symbol.T(Sym.LBRACKET), Symbol.V("E"), Symbol.T(Sym.RBRACKET))
                );

            return grammar;
        }
    }
}
