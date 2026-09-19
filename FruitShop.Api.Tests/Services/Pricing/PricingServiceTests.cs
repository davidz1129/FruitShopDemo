using FluentAssertions;
using FruitShop.Api.Models;
using FruitShop.Api.Services;
using FruitShop.Api.Services.ConditionEvaluator;
using FruitShop.Api.Services.PriceActionStrategy;
using Moq;

namespace FruitShop.Api.Tests;

public sealed class PricingServiceTests
{
    [Fact]
    public void CalculateUnitPrice_ShouldReturnBasePrice_WhenNoRulesApply()
    {
        // Arrange
        var factory = new Mock<IPricingStrategyFactory>();
        var service = new PricingService(factory.Object);

        // Act
        var result = service.CalculateUnitPrice(Context(), []);

        // Assert
        result.Should().Be(10m);
    }

    [Fact]
    public void CalculatePrice_ShouldSkipInactiveAndUnsatisfiedRules_WhenRulesDoNotApply()
    {
        // Arrange
        var factory = new Mock<IPricingStrategyFactory>();
        var evaluator = new Mock<IConditionEvaluator>();
        evaluator.Setup(candidate => candidate.IsSatisfied(It.IsAny<PriceRuleCondition>(), It.IsAny<PricingContext>())).Returns(false);
        factory.Setup(candidate => candidate.GetConditionEvaluator("quantity")).Returns(evaluator.Object);
        var inactive = new PriceRule { Name = "Inactive", Status = RuleStatus.Inactive };
        var unsatisfied = new PriceRule { Name = "Minimum", Conditions = [new PriceRuleCondition { Attribute = "quantity", Operator = ">=", Value = "5" }] };
        var service = new PricingService(factory.Object);

        // Act
        var result = service.CalculatePrice(Context(), [inactive, unsatisfied]);

        // Assert
        result.Should().BeEquivalentTo(new PriceCalculation(10m, "Base price $10.00 applied"));
        evaluator.Verify(candidate => candidate.IsSatisfied(unsatisfied.Conditions.Single(), It.IsAny<PricingContext>()), Times.Once);
    }

    [Fact]
    public void CalculatePrice_ShouldApplyDescriptionAndStop_WhenNonStackableRuleChangesPrice()
    {
        // Arrange
        var factory = new Mock<IPricingStrategyFactory>();
        var evaluator = new Mock<IConditionEvaluator>();
        var strategy = new Mock<IPriceActionStrategy>();
        evaluator.Setup(candidate => candidate.IsSatisfied(It.IsAny<PriceRuleCondition>(), It.IsAny<PricingContext>())).Returns(true);
        strategy.Setup(candidate => candidate.Apply(It.IsAny<PriceRuleAction>(), 10m, It.IsAny<PricingContext>())).Returns(8m);
        strategy.Setup(candidate => candidate.Describe(It.IsAny<PriceRuleAction>())).Returns("20% off");
        factory.Setup(candidate => candidate.GetConditionEvaluator("quantity")).Returns(evaluator.Object);
        factory.Setup(candidate => candidate.GetActionStrategy(ActionType.PercentageDiscount)).Returns(strategy.Object);
        var first = new PriceRule
        {
            Name = "VIP",
            IsStackable = false,
            Conditions = [new PriceRuleCondition { Attribute = "quantity", Operator = ">=", Value = "1" }],
            Actions = [new PriceRuleAction { ActionType = ActionType.PercentageDiscount, Amount = 20m }]
        };
        var second = new PriceRule { Name = "Never", Conditions = [new PriceRuleCondition { Attribute = "other", Operator = "=", Value = "x" }] };
        var service = new PricingService(factory.Object);

        // Act
        var result = service.CalculatePrice(Context(), [first, second]);

        // Assert
        result.Should().BeEquivalentTo(new PriceCalculation(8m, "Base price $10.00; adjusted by: VIP (20% off)"));
        factory.Verify(candidate => candidate.GetConditionEvaluator("other"), Times.Never);
        strategy.Verify(candidate => candidate.Apply(first.Actions.Single(), 10m, It.IsAny<PricingContext>()), Times.Once);
    }

    [Fact]
    public void CalculatePrice_ShouldApplyRunningTotalAmountOffOnceToTheOrderLine()
    {
        // Arrange
        var factory = new PricingStrategyFactory([], [new AmountOffStrategy()]);
        var service = new PricingService(factory);
        var context = new PricingContext(1, "RETAIL", 100m, new DateTime(2026, 9, 20), 10m);
        var rule = new PriceRule
        {
            Name = "Kiwi line discount",
            Actions = [new PriceRuleAction { ActionType = ActionType.AmountOff, Amount = 5m, CalculationBase = CalculationBase.RunningTotal }]
        };

        // Act
        var result = service.CalculatePrice(context, [rule]);

        // Assert
        result.UnitPriceApplied.Should().Be(9.95m);
        (result.UnitPriceApplied * context.Quantity).Should().Be(995m);
    }

    [Fact]
    public void CalculatePrice_ShouldClampPriceAtZero_WhenRuleCreatesNegativePrice()
    {
        // Arrange
        var factory = new Mock<IPricingStrategyFactory>();
        var strategy = new Mock<IPriceActionStrategy>();
        strategy.Setup(candidate => candidate.Apply(It.IsAny<PriceRuleAction>(), 10m, It.IsAny<PricingContext>())).Returns(-2m);
        strategy.Setup(candidate => candidate.Describe(It.IsAny<PriceRuleAction>())).Returns("$12 off");
        factory.Setup(candidate => candidate.GetActionStrategy(ActionType.AmountOff)).Returns(strategy.Object);
        var rule = new PriceRule { Name = "Clearance", Actions = [new PriceRuleAction { ActionType = ActionType.AmountOff, Amount = 12m }] };
        var service = new PricingService(factory.Object);

        // Act
        var result = service.CalculatePrice(Context(), [rule]);

        // Assert
        result.UnitPriceApplied.Should().Be(0m);
        result.PriceChangeReason.Should().Contain("Clearance ($12 off)");
    }

    private static PricingContext Context() => new(1, "RETAIL", 1m, new DateTime(2026, 9, 19), 10m);
}