using FluentAssertions;
using FruitShop.Api.Models;
using FruitShop.Api.Services.ConditionEvaluator;

namespace FruitShop.Api.Tests;

public sealed class CustomerTierConditionEvaluatorTests
{
    [Theory]
    [InlineData("vip", true)]
    [InlineData("WHOLESALE", false)]
    public void IsSatisfied_ShouldCompareCustomerTierIgnoringCase_WhenOperatorIsEqual(string value, bool expected)
    {
        // Arrange
        var evaluator = new CustomerTierConditionEvaluator();
        var condition = new PriceRuleCondition { Attribute = "customerTier", Operator = "=", Value = value };

        // Act
        var result = evaluator.IsSatisfied(condition, Context());

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void IsSatisfied_ShouldThrowNotSupportedException_WhenOperatorIsUnsupported()
    {
        // Arrange
        var evaluator = new CustomerTierConditionEvaluator();
        var condition = new PriceRuleCondition { Attribute = "customerTier", Operator = "!=", Value = "VIP" };

        // Act
        Action act = () => evaluator.IsSatisfied(condition, Context());

        // Assert
        act.Should().Throw<NotSupportedException>();
    }

    private static PricingContext Context() => new(1, "VIP", 5m, new DateTime(2026, 9, 19), 10m, 20m);
}