using FluentAssertions;
using FruitShop.Api.Models;
using FruitShop.Api.Services.ConditionEvaluator;

namespace FruitShop.Api.Tests;

public sealed class QuantityConditionEvaluatorTests
{
    [Theory]
    [InlineData(">=", "5", true)]
    [InlineData(">", "4", true)]
    [InlineData("<=", "5", true)]
    [InlineData("<", "6", true)]
    [InlineData("=", "5", true)]
    [InlineData(">", "5", false)]
    public void IsSatisfied_ShouldEvaluateQuantity_WhenOperatorIsSupported(string @operator, string value, bool expected)
    {
        // Arrange
        var evaluator = new QuantityConditionEvaluator();
        var condition = new PriceRuleCondition { Attribute = "quantity", Operator = @operator, Value = value };

        // Act
        var result = evaluator.IsSatisfied(condition, Context());

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void IsSatisfied_ShouldThrowNotSupportedException_WhenOperatorIsUnsupported()
    {
        // Arrange
        var evaluator = new QuantityConditionEvaluator();
        var condition = new PriceRuleCondition { Attribute = "quantity", Operator = "BETWEEN", Value = "5" };

        // Act
        Action act = () => evaluator.IsSatisfied(condition, Context());

        // Assert
        act.Should().Throw<NotSupportedException>();
    }

    private static PricingContext Context() => new(1, "RETAIL", 5m, new DateTime(2026, 9, 19), 10m, 20m);
}