using FruitShop.Api.Models;

namespace FruitShop.Api.Features.Orders;

/// <summary>Represents an order returned by the API.</summary>
public sealed record OrderResponse(
    long Id,
    string CustomerTier,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdateAt,
    OrderStatus Status,
    decimal TotalAmount,
    IReadOnlyList<OrderItemResponse> Items);

/// <summary>Represents an item within an order response.</summary>
public sealed record OrderItemResponse(
    long Id,
    long VariantId,
    string Sku,
    string UnitOfMeasure,
    decimal Quantity,
    decimal UnitPriceApplied,
    decimal TotalLineAmount,
    string PriceChangeReason,
    DateTimeOffset CreateAt,
    DateTimeOffset UpdateAt);