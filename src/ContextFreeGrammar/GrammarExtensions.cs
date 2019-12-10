using AutomataLib;

namespace ContextFreeGrammar
{
    public static class GrammarExtensions
    {
        public static Production Derives(this Nonterminal head, params Symbol[] tail)
        {
            return new Production(head, tail);
        }
    }
}
