using System;
using System.Collections.Generic;
using System.Diagnostics;
using AutomataLib;

namespace ContextFreeGrammar
{
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
    public struct LrGotoEntry : IEquatable<LrGotoEntry>
    {
        private string DebuggerDisplay => ToString();

        public readonly int SourceState;
        public readonly Nonterminal NonterminalSymbol;
        public readonly int TargetState;

        internal LrGotoEntry(int sourceState, Nonterminal nonterminalSymbol, int targetState)
        {
            SourceState = sourceState;
            NonterminalSymbol = nonterminalSymbol;
            TargetState = targetState;
        }

        public bool Equals(LrGotoEntry other)
        {
            return SourceState == other.SourceState &&
                   EqualityComparer<Nonterminal>.Default.Equals(NonterminalSymbol, other.NonterminalSymbol) &&
                   TargetState == other.TargetState;
        }

        public override bool Equals(object obj)
        {
            return obj is LrGotoEntry other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = SourceState.GetHashCode();
                hashCode = (hashCode * 397) ^ EqualityComparer<Nonterminal>.Default.GetHashCode(NonterminalSymbol);
                hashCode = (hashCode * 397) ^ TargetState.GetHashCode();
                return hashCode;
            }
        }

        public override string ToString()
        {
            return "(" + SourceState + "," + NonterminalSymbol + "," + TargetState + ")"; // tuple format
        }
    }
}
