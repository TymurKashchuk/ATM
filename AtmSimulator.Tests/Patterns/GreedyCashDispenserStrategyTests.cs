using AtmSimulator.Patterns.Strategy;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtmSimulator.Tests.Patterns
{
    public class GreedyCashDispenserStrategyTests
    {
        private readonly GreedyCashDispenserStrategy _strategy = new();

        private Dictionary<int, int> FullCash() => new()
    {
        { 1000, 10 }, { 500, 10 }, { 200, 10 },
        { 100, 10 },  { 50, 10 },  { 20, 10 }
    };

        [Fact]
        public void Calculate_ExactAmount_ReturnsCorrectBills()
        {
            var result = _strategy.Calculate(1000m, FullCash());

            result[1000].Should().Be(1);
        }

        [Fact]
        public void Calculate_MixedDenominations_ReturnsMinimumBills()
        {
            var result = _strategy.Calculate(1700m, FullCash());

            result[1000].Should().Be(1);
            result[500].Should().Be(1);
            result[200].Should().Be(1);
        }

        [Fact]
        public void Calculate_ImpossibleAmount_ThrowsException()
        {
            var limitedCash = new Dictionary<int, int> { { 1000, 1 } };

            var act = () => _strategy.Calculate(1500m, limitedCash);

            act.Should().Throw<InvalidOperationException>()
               .WithMessage("*неможливо*");
        }

        [Fact]
        public void Calculate_InsufficientBills_UsesAvailable()
        {
            var limitedCash = new Dictionary<int, int>
        {
            { 500, 1 }, { 200, 10 }
        };

            var result = _strategy.Calculate(900m, limitedCash);

            result[500].Should().Be(1);
            result[200].Should().Be(2);
        }

        [Fact]
        public void Calculate_ZeroAmount_ReturnsEmptyResult()
        {
            var result = _strategy.Calculate(0m, FullCash());

            result.Should().BeEmpty();
        }
    }
}
