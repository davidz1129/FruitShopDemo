using FluentAssertions;
using FruitShop.Api.Models;
using FruitShop.Api.Services;
using FruitShop.Api.Services.ConditionEvaluator;
using FruitShop.Api.Services.PriceActionStrategy;
using Moq;

namespace FruitShop.Api.Tests;

public sealed class PricingStrategyFactoryTests
{
    [Fact]
    public void GetConditionEvaluator_ShouldNormalizeAliases_WhenEvaluatorExists()
    {
        // Arrange
        var evaluator = new Mock<IConditionEvaluator>();
        evaluator.SetupGet(candidate => candidate.HandlesAttribute).Returns("orderDate");
        var factory = new PricingStrategyFactory([evaluator.Object], []);

        // Act
        var result = factory.GetConditionEvaluator(" Date ");

        // Assert
        result.Should().BeSameAs(evaluator.Object);
    }

    [Fact]
    public void GetConditionEvaluator_ShouldThrow_WhenAttributeIsNotRegistered()
    {
        // Arrange
        var factory = new PricingStrategyFactory([], []);

        // Act
        Action act = () => factory.GetConditionEvaluator("unknown");

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void GetActionStrategy_ShouldReturnMatchingStrategy_WhenActionTypeIsRegistered()
    {
        // Arrange
        var strategy = new Mock<IPriceActionStrategy>();
        strategy.SetupGet(candidate => candidate.HandlesAction).Returns(ActionType.AmountOff);
        var factory = new PricingStrategyFactory([], [strategy.Object]);

        // Act
        var result = factory.GetActionStrategy(ActionType.AmountOff);

        // Assert
        result.Should().BeSameAs(strategy.Object);
    }

    [Fact]
    public void GetActionStrategy_ShouldThrow_WhenActionTypeIsNotRegistered()
    {
        // Arrange
        var factory = new PricingStrategyFactory([], []);

        // Act
        Action act = () => factory.GetActionStrategy(ActionType.OverridePrice);

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }
}