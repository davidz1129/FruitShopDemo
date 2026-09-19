using FluentAssertions;
using FruitShop.Api.Models;
using FruitShop.Api.Services.PriceActionStrategy;

namespace FruitShop.Api.Tests;

public sealed class OverridePriceStrategyTests
{
    [Fact]
    public void Apply_ShouldReturnActionAmount_WhenOverridePriceIsConfigured()
    {
        // Arrange
        var strategy = new OverridePriceStrategy();
        var action = new PriceRuleAction { ActionType = ActionType.OverridePrice, Amount = 7.5m };

        // Act
        var result = strategy.Apply(action, 10m, 12m);

        // Assert
        result.Should().Be(7.5m);
    }

    [Fact]
    public void Describe_ShouldFormatOverrideAmount_WhenActionIsConfigured()
    {
        // Arrange
        var strategy = new OverridePriceStrategy();

        // Act
        var result = strategy.Describe(new PriceRuleAction { Amount = 7.5m });

        // Assert
        result.Should().Be("Override price to $7.5");
    }
}