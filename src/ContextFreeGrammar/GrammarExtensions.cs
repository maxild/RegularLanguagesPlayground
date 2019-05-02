using AutomataLib;

namespace ContextFreeGrammar
{
    public static class GrammarExtensions
    {
        public static Production<Nonterminal> GoesTo(this Nonterminal head, params Symbol[] tail)
        {
            return new Production<Nonterminal>(head, tail);
        }
    }
}
