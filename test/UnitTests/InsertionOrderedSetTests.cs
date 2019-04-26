using System;
using AutomataLib;
using Shouldly;
using Xunit;

namespace UnitTests
{
    public class InsertionOrderedSetTests
    {
        [Fact]
        public void Indexer()
        {
            var sut = new InsertionOrderedSet<int>(new []{ 2, 4, 3});
            sut[0].ShouldBe(2);
            sut[1].ShouldBe(4);
            sut[2].ShouldBe(3);
            Assert.Throws<ArgumentOutOfRangeException>(() => sut[3]);
        }
    }
}
