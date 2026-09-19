using System.ComponentModel.DataAnnotations;

namespace FruitShop.Api.Models;

/// <summary>Represents the data needed to administer the product catalog and its price rules.</summary>
public sealed record AdminCatalogResponse(
    PagedResult<AdminProductResponse> Products,
    IReadOnlyList<UnitOfMeasureResponse> UnitsOfMeasure,
    PagedResult<AdminPriceRuleResponse> PriceRules);

public sealed record AdminCatalogPageRequest
{
    [Range(1, PageRequest.MaximumPageNumber)]
    public int ProductPage { get; init; } = 1;

    [Range(1, PageRequest.MaximumPageSize)]
    public int ProductPageSize { get; init; } = PageRequest.DefaultPageSize;

    [Range(1, PageRequest.MaximumPageNumber)]
    public int PriceRulePage { get; init; } = 1;

    [Range(1, PageRequest.MaximumPageSize)]
    public int PriceRulePageSize { get; init; } = PageRequest.DefaultPageSize;

    public PageRequest ProductsPageRequest => new() { Page = ProductPage, PageSize = ProductPageSize };

    public PageRequest PriceRulesPageRequest => new() { Page = PriceRulePage, PageSize = PriceRulePageSize };
}

/// <summary>Represents a product in the catalog administration view.</summary>
public sealed record AdminProductResponse(long Id, string Name, IReadOnlyList<AdminProductVariantResponse> Variants);

/// <summary>Represents a product variant in the catalog administration view.</summary>
public sealed record AdminProductVariantResponse(
    long Id,
    string Sku,
    string UomCode,
    decimal UomFactor,
    decimal BasePrice,
    bool IsActive,
    MeasureType MeasureType);

/// <summary>Represents a unit of measure that can be assigned to a product variant.</summary>
public sealed record UnitOfMeasureResponse(string Code, MeasureType MeasureType);

/// <summary>Represents a price rule in the catalog administration view.</summary>
public sealed record AdminPriceRuleResponse(
    long Id,
    string Name,
    int Priority,
    bool IsStackable,
    bool AppliesToAllVariants,
    RuleStatus Status,
    IReadOnlyList<long> TargetVariantIds,
    IReadOnlyList<string> TargetCustomerTiers,
    IReadOnlyList<AdminPriceRuleConditionResponse> Conditions,
    IReadOnlyList<AdminPriceRuleActionResponse> Actions);

/// <summary>Represents the price rules assigned to one product variant and the rules available for assignment.</summary>
public sealed record ProductVariantPriceRulesResponse(
    AdminProductVariantResponse Variant,
    IReadOnlyList<AdminPriceRuleResponse> AssignedPriceRules,
    IReadOnlyList<AdminPriceRuleResponse> AvailablePriceRules,
    IReadOnlyList<AdminPriceRuleResponse> GlobalPriceRules);

/// <summary>Represents a condition attached to an administered price rule.</summary>
public sealed record AdminPriceRuleConditionResponse(string Attribute, string Operator, string Value);

/// <summary>Represents an action attached to an administered price rule.</summary>
public sealed record AdminPriceRuleActionResponse(ActionType ActionType, decimal Amount, CalculationBase CalculationBase);

/// <summary>Payload for creating a product from catalog administration.</summary>
public sealed record CreateAdminProductRequest
{
    public required string Name { get; init; }
}

/// <summary>Payload for creating a product variant from catalog administration.</summary>
public sealed record CreateProductVariantRequest
{
    public required string Sku { get; init; }

    public required string UomCode { get; init; }

    public required decimal UomFactor { get; init; }

    public required decimal BasePrice { get; init; }
}

/// <summary>Payload for changing whether a product variant can be ordered.</summary>
public sealed record SetProductVariantStatusRequest
{
    public bool? IsActive { get; init; }
}

/// <summary>Payload for creating a price rule and its conditions, targets, and actions.</summary>
public sealed record CreatePriceRuleRequest
{
    public required string Name { get; init; }

    public required int Priority { get; init; }

    public required bool IsStackable { get; init; }

    public required bool AppliesToAllVariants { get; init; }

    public IReadOnlyList<long> VariantIds { get; init; } = [];

    public IReadOnlyList<string> CustomerTiers { get; init; } = [];

    public IReadOnlyList<CreatePriceRuleConditionRequest> Conditions { get; init; } = [];

    public required IReadOnlyList<CreatePriceRuleActionRequest> Actions { get; init; }
}

/// <summary>Payload for adding a condition to a new price rule.</summary>
public sealed record CreatePriceRuleConditionRequest
{
    public required string Attribute { get; init; }

    public required string Operator { get; init; }

    public required string Value { get; init; }
}

/// <summary>Payload for adding an action to a new price rule.</summary>
public sealed record CreatePriceRuleActionRequest
{
    public required ActionType ActionType { get; init; }

    public required decimal Amount { get; init; }

    public CalculationBase CalculationBase { get; init; } = CalculationBase.RunningTotal;
}

public enum AdminCatalogOperationStatus
{
    Success,
    ProductNotFound,
    VariantNotFound,
    VariantInactive,
    UnitOfMeasureNotFound,
    DuplicateSku,
    InvalidRule,
    PriceRuleNotFound,
    PriceRuleAppliesToAllVariants,
    PriceRuleNotAssigned
}

public sealed record AdminCatalogOperationResult(AdminCatalogOperationStatus Status, string? Message = null);

public sealed record AdminCatalogOperationResult<T>(AdminCatalogOperationStatus Status, T? Value, string? Message = null);