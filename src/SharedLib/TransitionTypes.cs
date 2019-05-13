using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace AutomataLib
{
    public static class Transition
    {
        public static TAlphabet Epsilon<TAlphabet>()
        {
            Type t = typeof(TAlphabet);
            if (typeof(Symbol).IsAssignableFrom(t))
            {
                return (TAlphabet)(object)Symbol.Epsilon;
            }
            return default;
        }

        public static SourceTransitionPair<TState, TAlphabet> FromPair<TState, TAlphabet>(TState sourceState, TAlphabet label)
        {
            return new SourceTransitionPair<TState, TAlphabet>(sourceState, label);
        }

        public static SourceTransitionPair<TState, TAlphabet> FromEpsilonPair<TState, TAlphabet>(TState sourceState)
        {
            return new SourceTransitionPair<TState, TAlphabet>(sourceState, Epsilon<TAlphabet>());
        }

        public static TargetTransitionPair<TAlphabet, TState> ToPair<TAlphabet, TState>(TAlphabet label, TState targetState)
        {
            return new TargetTransitionPair<TAlphabet, TState>(label, targetState);
        }

        public static TargetTransitionPair<TAlphabet, TState> ToEpsilonPair<TAlphabet, TState>(TAlphabet label, TState targetState)
        {
            return new TargetTransitionPair<TAlphabet, TState>(Epsilon<TAlphabet>(), targetState);
        }

        public static Transition<TAlphabet, TState> Move<TAlphabet, TState>(
            TState sourceState,
            TAlphabet label,
            TState targetState)
        {
            return new Transition<TAlphabet, TState>(sourceState, label, targetState);
        }

        public static Transition<char, int> Move(
            int sourceState,
            char label,
            int targetState)
        {
            return new Transition<char, int>(sourceState, label, targetState);
        }

        public static Transition<TAlphabet, TState> EpsilonMove<TAlphabet, TState>(
            TState sourceState,
            TState targetState)
        {
            return new Transition<TAlphabet, TState>(sourceState, Epsilon<TAlphabet>(), targetState);
        }

        public static Transition<char, int> EpsilonMove(
            int sourceState,
            int targetState)
        {
            return new Transition<char, int>(sourceState, default, targetState);
        }
    }

    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
    public struct Transition<TAlphabet, TState> : IEquatable<Transition<TAlphabet, TState>>
    {
        private string DebuggerDisplay => ToString();

        public readonly TState SourceState;
        public readonly TState TargetState;
        public readonly TAlphabet Label;

        internal Transition(TState sourceState, TAlphabet label, TState targetState)
        {
            SourceState = sourceState;
            Label = label;
            TargetState = targetState;
        }

        public bool IsEpsilon => EqualityComparer<TAlphabet>.Default.Equals(Label, Transition.Epsilon<TAlphabet>());

        public bool Equals(Transition<TAlphabet, TState> other)
        {
            return EqualityComparer<TState>.Default.Equals(other.SourceState, SourceState) &&
                   EqualityComparer<TState>.Default.Equals(other.TargetState, TargetState) &&
                   EqualityComparer<TAlphabet>.Default.Equals(other.Label, Label);
        }

        public override bool Equals(object obj)
        {
            return obj is Transition<TAlphabet, TState> other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                // source mapped to (0,1,2,...,n)
                // target mapped to (0,2,4,...,2n)
                // label mapped to US-ASCII value (0,1,2,...,127), where zero/null is epsilon
                //return EqualityComparer<TState>.Default.GetHashCode(SourceState) +
                //       EqualityComparer<TState>.Default.GetHashCode(TargetState) * 2 +
                //       EqualityComparer<TAlphabet>.Default.GetHashCode(Label);

                var hashCode = EqualityComparer<TState>.Default.GetHashCode(SourceState);
                hashCode = (hashCode * 397) ^ EqualityComparer<TState>.Default.GetHashCode(TargetState);
                hashCode = (hashCode * 397) ^ EqualityComparer<TAlphabet>.Default.GetHashCode(Label);
                return hashCode;
            }
        }

        public override string ToString()
        {
            return "(" + SourceState + "," + (IsEpsilon ? "ε," : Label + ",") + TargetState + ")"; // tuple format
        }
    }

    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
    public struct TargetTransitionPair<TAlphabet, TState> : IEquatable<TargetTransitionPair<TAlphabet, TState>>
    {
        private string DebuggerDisplay => ToString();

        public readonly TAlphabet Label;
        public readonly TState TargetState;

        internal TargetTransitionPair(TAlphabet label, TState targetState)
        {
            Label = label;
            TargetState = targetState;
        }

        public bool IsEpsilon => EqualityComparer<TAlphabet>.Default.Equals(Label, Transition.Epsilon<TAlphabet>());

        public bool Equals(TargetTransitionPair<TAlphabet, TState> other)
        {
            return EqualityComparer<TState>.Default.Equals(other.TargetState, TargetState) &&
                   EqualityComparer<TAlphabet>.Default.Equals(other.Label, Label);
        }

        public override bool Equals(object obj)
        {
            return obj is TargetTransitionPair<TAlphabet, TState> other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = EqualityComparer<TAlphabet>.Default.GetHashCode(Label);
                hashCode = (hashCode * 397) ^ EqualityComparer<TState>.Default.GetHashCode(TargetState);
                return hashCode;
            }
        }

        public override string ToString()
        {
            return (IsEpsilon ? "(ε," : "(" + Label + ",") + TargetState + ")"; // tuple format
        }
    }

    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
    public struct SourceTransitionPair<TState, TAlphabet> : IEquatable<SourceTransitionPair<TState, TAlphabet>>
    {
        private string DebuggerDisplay => ToString();

        public readonly TState SourceState;
        public readonly TAlphabet Label;

        internal SourceTransitionPair(TState sourceState, TAlphabet label)
        {
            SourceState = sourceState;
            Label = label;
        }

        public bool IsEpsilon => EqualityComparer<TAlphabet>.Default.Equals(Label, Transition.Epsilon<TAlphabet>());

        public bool Equals(SourceTransitionPair<TState, TAlphabet> other)
        {
            return EqualityComparer<TState>.Default.Equals(other.SourceState, SourceState) &&
                   EqualityComparer<TAlphabet>.Default.Equals(other.Label, Label);
        }

        public override bool Equals(object obj)
        {
            return obj is SourceTransitionPair<TState, TAlphabet> other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = EqualityComparer<TAlphabet>.Default.GetHashCode(Label);
                hashCode = (hashCode * 397) ^ EqualityComparer<TState>.Default.GetHashCode(SourceState);
                return hashCode;
            }
        }

        public override string ToString()
        {
            return "(" + SourceState + (IsEpsilon ? ",ε)" : "," + Label + ")"); // tuple format
        }
    }
}
