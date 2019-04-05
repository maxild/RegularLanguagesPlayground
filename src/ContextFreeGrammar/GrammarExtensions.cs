using AutomataLib;

namespace ContextFreeGrammar
{
    public static class GrammarExtensions
    {
        public static Production GoesTo(this NonTerminal head, params Symbol[] tail)
        {
            return new Production(head, tail);
        }
    }
}
