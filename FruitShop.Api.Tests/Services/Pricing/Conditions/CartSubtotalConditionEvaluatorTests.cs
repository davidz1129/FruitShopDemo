using FluentAssertions;
using FruitShop.Api.Models;
using FruitShop.Api.Services.ConditionEvaluator;

namespace FruitShop.Api.Tests;

public sealed class CartSubtotalConditionEvaluatorTests
{
    [Theory]
    [InlineData(">=", "20", true)]
    [InlineData(">", "19", true)]
    [InlineData("<=", "20", true)]
    [InlineData("<", "21", true)]
    [InlineData("=", "20", true)]
    [InlineData("<", "20", false)]
    public void IsSatisfied_ShouldEvaluateCartSubtotal_WhenOperatorIsSupported(string @operator, string value, bool expected)
    {
        // Arrange
        var evaluator = new CartSubtotalConditionEvaluator();
        var condition = new PriceRuleCondition { Attribute = "cartSubtotal", Operator = @operator, Value = value };

        // Act
        var result = evaluator.IsSatisfied(condition, Context());

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void IsSatisfied_ShouldThrowNotSupportedException_WhenOperatorIsUnsupported()
    {
        // Arrange
        var evaluator = new CartSubtotalConditionEvaluator();
        var condition = new PriceRuleCondition { Attribute = "cartSubtotal", Operator = "BETWEEN", Value = "20" };

        // Act
        Action act = () => evaluator.IsSatisfied(condition, Context());

        // Assert
        act.Should().Throw<NotSupportedException>();
    }

    private static PricingContext Context() => new(1, "RETAIL", 5m, new DateTime(2026, 9, 19), 10m, 20m);
}