using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AutomataLib;

namespace ContextFreeGrammar
{
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
    public class LrConflict<TTokenKind> where TTokenKind : struct, Enum
    {
        private string DebuggerDisplay => ToString();

        private readonly LrAction[] _actions;

        public LrConflict(int state, Terminal<TTokenKind> symbol, IEnumerable<LrAction> actions)
        {
            State = state;
            Symbol = symbol;
            _actions = actions.ToArray();
        }

        public int State { get; }

        public Terminal<TTokenKind> Symbol { get; }

        public IReadOnlyList<LrAction> Actions => _actions;

        public LrConflict<TTokenKind> WithAction(LrAction action)
        {
            return new LrConflict<TTokenKind>(State, Symbol, Actions.Concat(action.AsSingletonEnumerable()));
        }

        public override string ToString()
        {
            return $"State {State}: {Actions.ToVectorString()} on '{Symbol}'";
        }
    }
}
