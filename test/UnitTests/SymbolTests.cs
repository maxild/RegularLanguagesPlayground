using AutomataLib;
using Shouldly;
using Xunit;

namespace UnitTests
{
    public class SymbolTests
    {
        [Fact]
        public void Epsilon()
        {
            Symbol.Epsilon.IsTerminal.ShouldBeFalse();
            Symbol.Epsilon.IsNonTerminal.ShouldBeFalse();
        }

        [Fact]
        public void Eof()
        {
            Symbol.EofMarker.IsNonTerminal.ShouldBeFalse();
            Symbol.EofMarker.IsTerminal.ShouldBeTrue(); // by convention (most textbooks say false here)
        }
    }
}
