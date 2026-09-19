namespace FruitShop.Api.Models;

public record PricingContext(
    long VariantId,
    string CustomerTier,
    decimal Quantity,
    DateTime OrderDate,
    decimal OriginalBasePrice,
    decimal CartSubtotal = 0
);

public record PriceCalculation(decimal UnitPriceApplied, string PriceChangeReason);