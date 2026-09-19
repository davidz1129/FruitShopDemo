using FruitShop.Api.Controllers;
using FruitShop.Api.Features.Orders;
using FruitShop.Api.Models;
using FruitShop.Api.Validation;

namespace FruitShop.Api.Tests;

public sealed class ControllerRequestValidatorTests
{
    [Fact]
    public void Validators_accept_valid_controller_requests()
    {
        Assert.True(new SubmitOrderRequestValidator().Validate(new SubmitOrderRequest
        {
            CustomerTier = " retail ",
            Items = [new SubmitOrderItemRequest { VariantId = 1, Quantity = 1 }]
        }).IsValid);
        Assert.True(new CalculateOrderItemRequestValidator().Validate(new CalculateOrderItemRequest
        {
            CustomerTier = "VIP",
            VariantId = 1,
            Quantity = 0,
            CartSubtotal = 0
        }).IsValid);
        Assert.True(new CreateAdminProductRequestValidator().Validate(new CreateAdminProductRequest { Name = "Apples" }).IsValid);
        Assert.True(new CreateProductVariantRequestValidator().Validate(new CreateProductVariantRequest
        {
            Sku = "APPLE-1KG",
            UomCode = "kg",
            UomFactor = 1,
            BasePrice = 2.99m
        }).IsValid);
        Assert.True(new SetProductVariantStatusRequestValidator().Validate(new SetProductVariantStatusRequest
        {
            IsActive = false
        }).IsValid);
        Assert.True(new CreatePriceRuleRequestValidator().Validate(new CreatePriceRuleRequest
        {
            Name = "VIP discount",
            Priority = 1,
            IsStackable = true,
            AppliesToAllVariants = false,
            VariantIds = [1],
            CustomerTiers = ["VIP"],
            Conditions = [new CreatePriceRuleConditionRequest
            {
                Attribute = "CustomerTier",
                Operator = "=",
                Value = "VIP"
            }],
            Actions = [new CreatePriceRuleActionRequest
            {
                ActionType = ActionType.PercentageDiscount,
                Amount = 10
            }]
        }).IsValid);
    }

    [Fact]
    public void Submit_order_validator_rejects_an_unsupported_tier_and_invalid_line_item()
    {
        var result = new SubmitOrderRequestValidator().Validate(new SubmitOrderRequest
        {
            CustomerTier = "GUEST",
            Items = [new SubmitOrderItemRequest { VariantId = 0, Quantity = 0 }]
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(SubmitOrderRequest.CustomerTier));
        Assert.Contains(result.Errors, error => error.PropertyName == "Items[0].VariantId");
        Assert.Contains(result.Errors, error => error.PropertyName == "Items[0].Quantity");
    }

    [Fact]
    public void Price_rule_validator_requires_a_target_when_not_global_and_valid_nested_values()
    {
        var result = new CreatePriceRuleRequestValidator().Validate(new CreatePriceRuleRequest
        {
            Name = " ",
            Priority = 10001,
            IsStackable = false,
            AppliesToAllVariants = false,
            VariantIds = [],
            CustomerTiers = [""],
            Conditions = [new CreatePriceRuleConditionRequest
            {
                Attribute = "",
                Operator = "",
                Value = ""
            }],
            Actions = [new CreatePriceRuleActionRequest
            {
                ActionType = (ActionType)999,
                Amount = -1,
                CalculationBase = (CalculationBase)999
            }]
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreatePriceRuleRequest.VariantIds));
        Assert.Contains(result.Errors, error => error.PropertyName == "Conditions[0].Attribute");
        Assert.Contains(result.Errors, error => error.PropertyName == "Actions[0].ActionType");
    }

    [Theory]
    [InlineData("Unknown", "=", "1", "Attribute")]
    [InlineData("Quantity", "BETWEEN", "1", "Operator")]
    [InlineData("CartSubtotal", ">=", "twenty", "Value")]
    [InlineData("CustomerTier", "=", "GUEST", "Value")]
    [InlineData("OrderDate", "=", "09/18/2026", "Value")]
    [InlineData("OrderDate", "BETWEEN", "2026-10-01 AND 2026-09-01", "Value")]
    public void Price_rule_validator_rejects_unsupported_or_malformed_conditions(
        string attribute,
        string @operator,
        string value,
        string invalidProperty)
    {
        var result = new CreatePriceRuleConditionRequestValidator().Validate(new CreatePriceRuleConditionRequest
        {
            Attribute = attribute,
            Operator = @operator,
            Value = value
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == invalidProperty);
    }

    [Theory]
    [InlineData("Quantity", ">=", "3.5")]
    [InlineData("CartSubtotal", "GreaterThan", "25.00")]
    [InlineData("CustomerTier", "=", "vip")]
    [InlineData("OrderDate", "BETWEEN", "2026-09-01 AND 2026-09-30")]
    [InlineData("Date", "LessThanOrEqual", "2026-09-30")]
    public void Price_rule_validator_accepts_supported_condition_combinations(string attribute, string @operator, string value)
    {
        var result = new CreatePriceRuleConditionRequestValidator().Validate(new CreatePriceRuleConditionRequest
        {
            Attribute = attribute,
            Operator = @operator,
            Value = value
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Variant_status_validator_requires_an_explicit_status()
    {
        var result = new SetProductVariantStatusRequestValidator().Validate(new SetProductVariantStatusRequest());

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(SetProductVariantStatusRequest.IsActive));
    }
}