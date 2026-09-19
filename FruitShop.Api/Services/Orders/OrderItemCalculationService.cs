using FruitShop.Api.Models;
using FruitShop.Api.Repositories;

namespace FruitShop.Api.Services;

public sealed class OrderItemCalculationService(
    IProductRepository productRepository,
    IPriceRuleRepository priceRuleRepository,
    IPricingService pricingService) : IOrderItemCalculationService
{
    public async Task<OrderItemQuote> QuoteAsync(
        OrderItemQuoteRequest request,
        CancellationToken cancellationToken = default)
    {
        string normalizedCustomerTier = request.CustomerTier.Trim().ToUpperInvariant();
        var activeRules = await priceRuleRepository.GetActiveRulesAsync(
            request.Variant.Id,
            normalizedCustomerTier,
            cancellationToken);

        var pricingContext = new PricingContext(
            request.Variant.Id,
            normalizedCustomerTier,
            request.Quantity,
            request.OrderDate,
            request.Variant.BasePrice,
            request.CartSubtotal);
        var priceCalculation = pricingService.CalculatePrice(pricingContext, activeRules);

        return new OrderItemQuote(priceCalculation, activeRules);
    }

    public async Task<OrderItemPreview?> CalculateAsync(
        long variantId,
        string customerTier,
        decimal quantity,
        decimal cartSubtotal,
        CancellationToken cancellationToken = default)
    {
        var variant = await productRepository.GetVariantByIdAsync(variantId, cancellationToken);
        if (variant is null)
        {
            return null;
        }

        var quote = await QuoteAsync(
            new OrderItemQuoteRequest(
                variant,
                customerTier,
                quantity,
                cartSubtotal,
                DateTime.UtcNow),
            cancellationToken);

        return new OrderItemPreview(variant, quantity, quote.PriceCalculation, quote.ActiveRules);
    }
}