using System;
using System.Diagnostics.CodeAnalysis;
using AutomataLib;
using Shouldly;
using Xunit;

namespace UnitTests
{
    public class EnumUtilsTests
    {
        public class GrammarVariable : IIndexerValue
        {
            public GrammarVariable(string name, int index)
            {
                Name = name;
                Index = index;
            }
            public string Name { get; }
            public int Index { get; }
        }

        public class TokenVariable<TTokenKind> : IIndexerValue
            where TTokenKind : struct, Enum
        {
            public TokenVariable(string name, int index, TTokenKind kind)
            {
                Name = name;
                Index = index;
                Kind = kind;
            }

            public TTokenKind Kind { get; }
            public string Name { get; }
            public int Index { get; }
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        enum Sym
        {
            EPS = -2,
            NIL = -1,
            EOF = 0,
            ID,
            NUM,
            LPARAN,
            RPARAN
        }

        [Fact]
        public void ValidEnum()
        {
            var grammarVariables = EnumUtils.MapToIndexValues<Sym, GrammarVariable>((_, name, index) =>
                new GrammarVariable(name, index));
            grammarVariables.Count.ShouldBe(5);
            grammarVariables[0].Name.ShouldBe("EOF");
            grammarVariables[0].Index.ShouldBe(0);
            grammarVariables[1].Name.ShouldBe("ID");
            grammarVariables[1].Index.ShouldBe(1);
            grammarVariables[2].Name.ShouldBe("NUM");
            grammarVariables[2].Index.ShouldBe(2);
            grammarVariables[3].Name.ShouldBe("LPARAN");
            grammarVariables[3].Index.ShouldBe(3);
            grammarVariables[4].Name.ShouldBe("RPARAN");
            grammarVariables[4].Index.ShouldBe(4);
        }

        [Fact]
        public void ValidEnumWithEnumValue()
        {
            var tokenVariables = EnumUtils.MapToIndexValues<Sym, TokenVariable<Sym>>((enumValue, name, index) =>
                new TokenVariable<Sym>(name, index, enumValue));
            tokenVariables.Count.ShouldBe(5);
            tokenVariables[0].Name.ShouldBe("EOF");
            tokenVariables[0].Index.ShouldBe(0);
            tokenVariables[0].Kind.ShouldBe(Sym.EOF);
            tokenVariables[1].Name.ShouldBe("ID");
            tokenVariables[1].Index.ShouldBe(1);
            tokenVariables[1].Kind.ShouldBe(Sym.ID);
            tokenVariables[2].Name.ShouldBe("NUM");
            tokenVariables[2].Index.ShouldBe(2);
            tokenVariables[2].Kind.ShouldBe(Sym.NUM);
            tokenVariables[3].Name.ShouldBe("LPARAN");
            tokenVariables[3].Index.ShouldBe(3);
            tokenVariables[3].Kind.ShouldBe(Sym.LPARAN);
            tokenVariables[4].Name.ShouldBe("RPARAN");
            tokenVariables[4].Index.ShouldBe(4);
            tokenVariables[4].Kind.ShouldBe(Sym.RPARAN);
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        enum Dup
        {
            EPS = 0,
            EOF = 0
        }

        [Fact]
        public void DuplicateValues_ThrowsInvalidOperation()
        {
            Assert.Throws<InvalidOperationException>(() => EnumUtils.MapToIndexValues<Dup, TokenVariable<Dup>>(
                    (enumValue, name, index) =>
                        new TokenVariable<Dup>(name, index, enumValue)))
                .Message
                .ShouldBe(
                    "Duplicate values are not supported --- check the specification of UnitTests.EnumUtilsTests+Dup.");
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        enum NonCont
        {
            EPS = 0,
            EOF = 2
        }

        [Fact]
        public void NonContiguousValues_ThrowsInvalidOperation()
        {
            Assert.Throws<InvalidOperationException>(() => EnumUtils.MapToIndexValues<NonCont, TokenVariable<NonCont>>(
                    (enumValue, name, index) =>
                        new TokenVariable<NonCont>(name, index, enumValue)))
                .Message
                .ShouldBe(
                    "The non-negative values must form a contiguous range 0,1,2,...,N-1 --- check the specification of UnitTests.EnumUtilsTests+NonCont.");

        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        enum Wrong : byte
        {
            EPS = 0
        }

        [Fact]
        public void WrongUnderlyingType_ThrowsInvalidOperation()
        {
            Assert.Throws<InvalidOperationException>(() => EnumUtils.MapToIndexValues<Wrong, TokenVariable<Wrong>>(
                    (enumValue, name, index) =>
                        new TokenVariable<Wrong>(name, index, enumValue)))
                .Message
                .ShouldBe(
                    "Only enums with an underlying type of System.Int32 are supported --- check the specification of UnitTests.EnumUtilsTests+Wrong.");

        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        [Flags]
        enum Flags
        {
            EPS = 0
        }

        [Fact]
        public void FlagsEnum_ThrowsInvalidOperation()
        {
            Assert.Throws<InvalidOperationException>(() => EnumUtils.MapToIndexValues<Flags, TokenVariable<Flags>>(
                    (enumValue, name, index) =>
                        new TokenVariable<Flags>(name, index, enumValue)))
                .Message
                .ShouldBe(
                    "Flags enums are not supported --- check the specification of UnitTests.EnumUtilsTests+Flags.");

        }
    }
}
