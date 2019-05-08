using System;
using System.Diagnostics;
using JetBrains.Annotations;

namespace AutomataLib
{
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
    public struct LrAction : IEquatable<LrAction>
    {
        enum LrActionKind
        {
            Error = 0,
            Shift,          // Si, where i is a state
            Reduce,         // Rj, where j is production index
            Accept
        }

        private string DebuggerDisplay => ToString();

        private readonly LrActionKind _kind;
        private readonly int _value;

        public static readonly LrAction Error = new LrAction(LrActionKind.Error, 0);

        public static readonly LrAction Accept = new LrAction(LrActionKind.Accept, 0);

        public static LrAction Reduce(int productionIndex)
        {
            return new LrAction(LrActionKind.Reduce, productionIndex);
        }

        public static LrAction Shift(int stateIndex)
        {
            return new LrAction(LrActionKind.Shift, stateIndex);
        }

        private LrAction(LrActionKind kind, int value)
        {
            _kind = kind;
            _value = value;
        }

        public bool IsShift => _kind == LrActionKind.Shift;
        public bool IsReduce => _kind == LrActionKind.Reduce;
        public bool IsAccept => _kind == LrActionKind.Accept;
        public bool IsError => _kind == LrActionKind.Error;

        public int ShiftToState => _value;

        public int ReduceToProductionIndex => _value;

        public override string ToString()
        {
            switch (_kind)
            {
                case LrActionKind.Shift:
                    return $"shift {_value}";
                case LrActionKind.Reduce:
                    return $"reduce {_value}";
                default:
                    return _kind.ToString().ToLower();
            }
        }

        public string ToTableString()
        {
            switch (_kind)
            {
                case LrActionKind.Shift:
                    return $"s{_value}";
                case LrActionKind.Reduce:
                    return $"r{_value}";
                case LrActionKind.Accept:
                    return "acc";
                case LrActionKind.Error:
                    return string.Empty;
                default:
                    return _kind.ToString().ToLower();
            }
        }

        [Pure]
        public bool Equals(LrAction other)
        {
            return _kind == other._kind && _value == other._value;
        }

        public override bool Equals(object obj)
        {
            return obj is LrAction other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int) _kind * 397) ^ _value;
            }
        }
    }
}
