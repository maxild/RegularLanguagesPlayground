using System;
using System.Collections.Generic;
using System.Diagnostics;
using AutomataLib;

namespace ContextFreeGrammar
{
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
    public struct LrActionEntry<TTerminalSymbol> : IEquatable<LrActionEntry<TTerminalSymbol>>
        where TTerminalSymbol : Symbol, IEquatable<TTerminalSymbol>
    {
        private string DebuggerDisplay => ToString();

        public readonly int State;
        public readonly TTerminalSymbol TerminalSymbol;
        public readonly LrAction Action;

        internal LrActionEntry(int state, TTerminalSymbol terminalSymbol, LrAction action)
        {
            State = state;
            TerminalSymbol = terminalSymbol;
            Action = action;
        }

        public bool Equals(LrActionEntry<TTerminalSymbol> other)
        {
            return State == other.State &&
                   EqualityComparer<TTerminalSymbol>.Default.Equals(TerminalSymbol, other.TerminalSymbol) &&
                   Action.Equals(other.Action);
        }

        public override bool Equals(object obj)
        {
            return obj is LrActionEntry<TTerminalSymbol> other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = State.GetHashCode();
                hashCode = (hashCode * 397) ^ EqualityComparer<TTerminalSymbol>.Default.GetHashCode(TerminalSymbol);
                hashCode = (hashCode * 397) ^ Action.GetHashCode();
                return hashCode;
            }
        }

        public override string ToString()
        {
            return "(" + State + "," + TerminalSymbol + "," + Action + ")"; // tuple format
        }
    }
}
