using AutoMapper;
using FruitShop.Api.Services;
using MediatR;

namespace FruitShop.Api.Features.Orders;

/// <summary>Payload for calculating an order item without saving it.</summary>
public sealed record CalculateOrderItemRequest
{
    public required string CustomerTier { get; init; }

    public required long VariantId { get; init; }

    public required decimal Quantity { get; init; }

    public required decimal CartSubtotal { get; init; }
}

public sealed record CalculateOrderItemQuery(
    long VariantId,
    string CustomerTier,
    decimal Quantity,
    decimal CartSubtotal) : IRequest<CalculateOrderItemResponse?>;

public sealed class CalculateOrderItemQueryHandler(IOrderItemCalculationService orderItemCalculationService, IMapper mapper)
    : IRequestHandler<CalculateOrderItemQuery, CalculateOrderItemResponse?>
{
    public async Task<CalculateOrderItemResponse?> Handle(
        CalculateOrderItemQuery request,
        CancellationToken cancellationToken)
    {
        var preview = await orderItemCalculationService.CalculateAsync(
            request.VariantId,
            request.CustomerTier,
            request.Quantity,
            request.CartSubtotal,
            cancellationToken);

        return preview is null ? null : mapper.Map<CalculateOrderItemResponse>(preview);
    }
}

/// <summary>Represents a calculated order item that has not been saved.</summary>
public sealed record CalculateOrderItemResponse(
    long VariantId,
    string Sku,
    string UnitOfMeasure,
    decimal BaseUnitPrice,
    decimal Quantity,
    decimal UnitPriceApplied,
    decimal LineSubtotal,
    string PriceChangeReason,
    IReadOnlyList<RelatedPriceRuleResponse> ActivePriceRules);

/// <summary>Represents an active rule related to the selected variant and customer tier.</summary>
public sealed record RelatedPriceRuleResponse(
    long Id,
    string Name,
    int Priority,
    bool IsStackable,
    IReadOnlyList<PriceRuleConditionResponse> Conditions,
    IReadOnlyList<PriceRuleActionResponse> Actions);

/// <summary>Represents a condition attached to an active price rule.</summary>
public sealed record PriceRuleConditionResponse(string Attribute, string Operator, string Value);

/// <summary>Represents an action attached to an active price rule.</summary>
public sealed record PriceRuleActionResponse(string ActionType, decimal Amount, string CalculationBase);