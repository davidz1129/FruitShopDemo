using FruitShop.Api.Models;

namespace FruitShop.Api.Services;

public interface IOrderItemCalculationService
{
    Task<OrderItemQuote> QuoteAsync(
        OrderItemQuoteRequest request,
        CancellationToken cancellationToken = default);

    Task<OrderItemPreview?> CalculateAsync(
        long variantId,
        string customerTier,
        decimal quantity,
        decimal cartSubtotal,
        CancellationToken cancellationToken = default);
}

public sealed record OrderItemQuoteRequest(
    ProductVariant Variant,
    string CustomerTier,
    decimal Quantity,
    decimal CartSubtotal,
    DateTime OrderDate);

public sealed record OrderItemQuote(
    PriceCalculation PriceCalculation,
    IReadOnlyList<PriceRule> ActiveRules);

public sealed record OrderItemPreview(
    ProductVariant Variant,
    decimal Quantity,
    PriceCalculation PriceCalculation,
    IReadOnlyList<PriceRule> ActiveRules);