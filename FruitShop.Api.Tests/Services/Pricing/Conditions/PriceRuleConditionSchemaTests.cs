using FluentAssertions;
using FruitShop.Api.Services.ConditionEvaluator;

namespace FruitShop.Api.Tests;

public sealed class PriceRuleConditionSchemaTests
{
    [Theory]
    [InlineData("Quantity", "quantity")]
    [InlineData(" CartSubtotal ", "cartsubtotal")]
    [InlineData("Date", "orderdate")]
    public void NormalizeAttribute_ShouldNormalizeSupportedAliases_WhenAttributeHasWhitespaceOrAlias(string attribute, string expected)
    {
        // Arrange

        // Act
        var result = PriceRuleConditionSchema.NormalizeAttribute(attribute);

        // Assert
        result.Should().Be(expected);
        PriceRuleConditionSchema.IsSupportedAttribute(attribute).Should().BeTrue();
    }

    [Theory]
    [InlineData("CustomerTier", "=", true)]
    [InlineData("CustomerTier", ">=", false)]
    [InlineData("OrderDate", "BETWEEN", true)]
    [InlineData("Quantity", "BETWEEN", false)]
    [InlineData("Unknown", "=", false)]
    public void IsSupportedOperator_ShouldReturnExpectedResult_WhenAttributeAndOperatorAreProvided(string attribute, string @operator, bool expected)
    {
        // Arrange

        // Act
        var result = PriceRuleConditionSchema.IsSupportedOperator(attribute, @operator);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Quantity", ">=", "3.5", true)]
    [InlineData("CartSubtotal", "=", "not-a-number", false)]
    [InlineData("CustomerTier", "=", "vip", true)]
    [InlineData("CustomerTier", "=", "guest", false)]
    [InlineData("OrderDate", "=", "2026-09-19", true)]
    [InlineData("OrderDate", "BETWEEN", "2026-09-20 AND 2026-09-19", false)]
    public void HasValidValue_ShouldValidateValueByAttributeAndOperator_WhenConditionIsDefined(string attribute, string @operator, string value, bool expected)
    {
        // Arrange

        // Act
        var result = PriceRuleConditionSchema.HasValidValue(attribute, @operator, value);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void ParseMethods_ShouldReturnParsedValuesAndThrowFormatException_WhenInputIsInvalid()
    {
        // Arrange

        // Act
        var decimalResult = PriceRuleConditionSchema.ParseDecimal("12.50");
        var dateResult = PriceRuleConditionSchema.ParseDate("2026-09-19");
        var rangeResult = PriceRuleConditionSchema.ParseDateRange("2026-09-18 AND 2026-09-20");
        Action decimalAct = () => PriceRuleConditionSchema.ParseDecimal("not-a-number");
        Action dateAct = () => PriceRuleConditionSchema.ParseDate("19/09/2026");
        Action rangeAct = () => PriceRuleConditionSchema.ParseDateRange("2026-09-20 AND 2026-09-19");

        // Assert
        decimalResult.Should().Be(12.5m);
        dateResult.Should().Be(new DateTime(2026, 9, 19));
        rangeResult.Should().Be((new DateTime(2026, 9, 18), new DateTime(2026, 9, 20)));
        decimalAct.Should().Throw<FormatException>();
        dateAct.Should().Throw<FormatException>();
        rangeAct.Should().Throw<FormatException>();
    }
}