using FluentAssertions;
using FruitShop.Api.Models;
using FruitShop.Api.Services.PriceActionStrategy;

namespace FruitShop.Api.Tests;

public sealed class PercentageDiscountStrategyTests
{
    [Theory]
    [InlineData(CalculationBase.OriginalBase, 60)]
    [InlineData(CalculationBase.RunningTotal, 64)]
    public void Apply_ShouldDiscountUsingSelectedCalculationBase_WhenActionIsConfigured(CalculationBase calculationBase, decimal expected)
    {
        // Arrange
        var strategy = new PercentageDiscountStrategy();
        var action = new PriceRuleAction { ActionType = ActionType.PercentageDiscount, Amount = 20m, CalculationBase = calculationBase };

        // Act
        var result = strategy.Apply(action, 80m, 100m);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void Describe_ShouldFormatDiscountPercentage_WhenActionHasFractionalAmount()
    {
        // Arrange
        var strategy = new PercentageDiscountStrategy();

        // Act
        var result = strategy.Describe(new PriceRuleAction { Amount = 12.5m });

        // Assert
        result.Should().Be("12.5% off");
    }
}