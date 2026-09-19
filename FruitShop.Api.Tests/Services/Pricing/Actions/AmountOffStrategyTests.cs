using FluentAssertions;
using FruitShop.Api.Models;
using FruitShop.Api.Services.PriceActionStrategy;

namespace FruitShop.Api.Tests;

public sealed class AmountOffStrategyTests
{
    [Theory]
    [InlineData(CalculationBase.OriginalBase, 95)]
    [InlineData(CalculationBase.RunningTotal, 75)]
    public void Apply_ShouldSubtractFromSelectedCalculationBase_WhenActionIsConfigured(CalculationBase calculationBase, decimal expected)
    {
        // Arrange
        var strategy = new AmountOffStrategy();
        var action = new PriceRuleAction { ActionType = ActionType.AmountOff, Amount = 5m, CalculationBase = calculationBase };

        // Act
        var result = strategy.Apply(action, 80m, 100m);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void Apply_ShouldSubtractRunningTotalAmountOnceFromTheOrderLine()
    {
        // Arrange
        var strategy = new AmountOffStrategy();
        var action = new PriceRuleAction { ActionType = ActionType.AmountOff, Amount = 5m, CalculationBase = CalculationBase.RunningTotal };
        var context = new PricingContext(1, "RETAIL", 100m, new DateTime(2026, 9, 20), 10m);

        // Act
        var unitPrice = strategy.Apply(action, 10m, context);

        // Assert
        unitPrice.Should().Be(9.95m);
        (unitPrice * context.Quantity).Should().Be(995m);
    }

    [Fact]
    public void Describe_ShouldFormatCurrencyAmount_WhenActionHasFractionalAmount()
    {
        // Arrange
        var strategy = new AmountOffStrategy();

        // Act
        var result = strategy.Describe(new PriceRuleAction { Amount = 2.75m });

        // Assert
        result.Should().Be("$2.75 off (Running Total)");
    }
}