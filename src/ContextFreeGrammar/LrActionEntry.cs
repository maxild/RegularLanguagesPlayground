using System;
using System.Collections.Generic;
using System.Diagnostics;
using AutomataLib;

namespace ContextFreeGrammar
{
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
    public struct LrActionEntry<TTokenKind> : IEquatable<LrActionEntry<TTokenKind>>
        where TTokenKind : Enum
    {
        private string DebuggerDisplay => ToString();

        public readonly int State;
        public readonly Terminal<TTokenKind> TerminalSymbol;
        public readonly LrAction Action;

        internal LrActionEntry(int state, Terminal<TTokenKind> terminalSymbol, LrAction action)
        {
            State = state;
            TerminalSymbol = terminalSymbol;
            Action = action;
        }

        public bool Equals(LrActionEntry<TTokenKind> other)
        {
            return State == other.State &&
                   EqualityComparer<Terminal<TTokenKind>>.Default.Equals(TerminalSymbol, other.TerminalSymbol) &&
                   Action.Equals(other.Action);
        }

        public override bool Equals(object obj)
        {
            return obj is LrActionEntry<TTokenKind> other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = State.GetHashCode();
                hashCode = (hashCode * 397) ^ EqualityComparer<Terminal<TTokenKind>>.Default.GetHashCode(TerminalSymbol);
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
