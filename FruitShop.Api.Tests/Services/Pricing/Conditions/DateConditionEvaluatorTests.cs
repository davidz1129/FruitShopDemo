using FluentAssertions;
using FruitShop.Api.Models;
using FruitShop.Api.Services.ConditionEvaluator;

namespace FruitShop.Api.Tests;

public sealed class DateConditionEvaluatorTests
{
    [Theory]
    [InlineData(">=", "2026-09-19", true)]
    [InlineData(">", "2026-09-18", true)]
    [InlineData("<=", "2026-09-19", true)]
    [InlineData("<", "2026-09-20", true)]
    [InlineData("=", "2026-09-19", true)]
    [InlineData("BETWEEN", "2026-09-18 AND 2026-09-20", true)]
    [InlineData("BETWEEN", "2026-09-20 AND 2026-09-21", false)]
    public void IsSatisfied_ShouldEvaluateOrderDate_WhenOperatorIsSupported(string @operator, string value, bool expected)
    {
        // Arrange
        var evaluator = new DateConditionEvaluator();
        var condition = new PriceRuleCondition { Attribute = "orderDate", Operator = @operator, Value = value };

        // Act
        var result = evaluator.IsSatisfied(condition, Context());

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void IsSatisfied_ShouldThrowNotSupportedException_WhenOperatorIsUnsupported()
    {
        // Arrange
        var evaluator = new DateConditionEvaluator();
        var condition = new PriceRuleCondition { Attribute = "orderDate", Operator = "CONTAINS", Value = "2026-09-19" };

        // Act
        Action act = () => evaluator.IsSatisfied(condition, Context());

        // Assert
        act.Should().Throw<NotSupportedException>();
    }

    private static PricingContext Context() => new(1, "RETAIL", 5m, new DateTime(2026, 9, 19), 10m, 20m);
}