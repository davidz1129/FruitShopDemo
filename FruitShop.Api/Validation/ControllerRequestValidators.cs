using FluentValidation;
using FruitShop.Api.Controllers;
using FruitShop.Api.Features.Orders;
using FruitShop.Api.Models;
using FruitShop.Api.Services.ConditionEvaluator;

namespace FruitShop.Api.Validation;

public sealed class SubmitOrderRequestValidator : AbstractValidator<SubmitOrderRequest>
{
    public SubmitOrderRequestValidator()
    {
        RuleFor(request => request.CustomerTier)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MaximumLength(50)
            .Must(BeASupportedCustomerTier)
            .WithMessage("Customer tier must be RETAIL, VIP, or WHOLESALE.");

        RuleFor(request => request.Items)
            .NotNull()
            .NotEmpty();

        RuleForEach(request => request.Items)
            .SetValidator(new SubmitOrderItemRequestValidator());
    }

    private static bool BeASupportedCustomerTier(string customerTier) =>
        new[] { "RETAIL", "VIP", "WHOLESALE" }.Contains(customerTier.Trim(), StringComparer.OrdinalIgnoreCase);
}

public sealed class SubmitOrderItemRequestValidator : AbstractValidator<SubmitOrderItemRequest>
{
    public SubmitOrderItemRequestValidator()
    {
        RuleFor(request => request.VariantId).GreaterThan(0);
        RuleFor(request => request.Quantity).GreaterThan(0);
    }
}

public sealed class CalculateOrderItemRequestValidator : AbstractValidator<CalculateOrderItemRequest>
{
    public CalculateOrderItemRequestValidator()
    {
        RuleFor(request => request.CustomerTier)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MaximumLength(50)
            .Must(BeASupportedCustomerTier)
            .WithMessage("Customer tier must be RETAIL, VIP, or WHOLESALE.");
        RuleFor(request => request.VariantId).GreaterThan(0);
        RuleFor(request => request.Quantity).GreaterThanOrEqualTo(0);
        RuleFor(request => request.CartSubtotal).GreaterThanOrEqualTo(0);
    }

    private static bool BeASupportedCustomerTier(string customerTier) =>
        new[] { "RETAIL", "VIP", "WHOLESALE" }.Contains(customerTier.Trim(), StringComparer.OrdinalIgnoreCase);
}

public sealed class CreateAdminProductRequestValidator : AbstractValidator<CreateAdminProductRequest>
{
    public CreateAdminProductRequestValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty()
            .MaximumLength(255);
    }
}

public sealed class CreateProductVariantRequestValidator : AbstractValidator<CreateProductVariantRequest>
{
    public CreateProductVariantRequestValidator()
    {
        RuleFor(request => request.Sku)
            .NotEmpty()
            .MaximumLength(100);
        RuleFor(request => request.UomCode)
            .NotEmpty()
            .MaximumLength(20);
        RuleFor(request => request.UomFactor).GreaterThan(0);
        RuleFor(request => request.BasePrice).GreaterThanOrEqualTo(0);
    }
}

public sealed class SetProductVariantStatusRequestValidator : AbstractValidator<SetProductVariantStatusRequest>
{
    public SetProductVariantStatusRequestValidator()
    {
        RuleFor(request => request.IsActive).NotNull();
    }
}

public sealed class CreatePriceRuleRequestValidator : AbstractValidator<CreatePriceRuleRequest>
{
    public CreatePriceRuleRequestValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty()
            .MaximumLength(255);
        RuleFor(request => request.Priority)
            .InclusiveBetween(0, 10000);
        RuleFor(request => request.VariantIds)
            .NotNull()
            .Must((request, variantIds) => request.AppliesToAllVariants || variantIds is { Count: > 0 })
            .WithMessage("Select at least one product variant or apply the rule to all variants.");
        RuleForEach(request => request.VariantIds)
            .GreaterThan(0);
        RuleFor(request => request.CustomerTiers)
            .NotNull();
        RuleForEach(request => request.CustomerTiers)
            .NotEmpty()
            .MaximumLength(50);
        RuleFor(request => request.Conditions)
            .NotNull();
        RuleForEach(request => request.Conditions)
            .SetValidator(new CreatePriceRuleConditionRequestValidator());
        RuleFor(request => request.Actions)
            .NotNull()
            .NotEmpty();
        RuleForEach(request => request.Actions)
            .SetValidator(new CreatePriceRuleActionRequestValidator());
    }
}

public sealed class CreatePriceRuleConditionRequestValidator : AbstractValidator<CreatePriceRuleConditionRequest>
{
    public CreatePriceRuleConditionRequestValidator()
    {
        RuleFor(request => request.Attribute)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MaximumLength(50)
            .Must(PriceRuleConditionSchema.IsSupportedAttribute)
            .WithMessage("Condition attribute must be Quantity, CartSubtotal, CustomerTier, or OrderDate.");
        RuleFor(request => request.Operator)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MaximumLength(20)
            .Must((request, @operator) => PriceRuleConditionSchema.IsSupportedOperator(request.Attribute, @operator))
            .WithMessage("Condition operator is not supported for the selected attribute.");
        RuleFor(request => request.Value)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must((request, value) => PriceRuleConditionSchema.HasValidValue(request.Attribute, request.Operator, value))
            .WithMessage("Condition value must match the selected attribute and operator.");
    }
}

public sealed class CreatePriceRuleActionRequestValidator : AbstractValidator<CreatePriceRuleActionRequest>
{
    public CreatePriceRuleActionRequestValidator()
    {
        RuleFor(request => request.ActionType).IsInEnum();
        RuleFor(request => request.Amount).GreaterThanOrEqualTo(0);
        RuleFor(request => request.CalculationBase).IsInEnum();
    }
}