using FruitShop.Api.Models;

namespace FruitShop.Api.Services;

public interface IPricingService
{
    PriceCalculation CalculatePrice(PricingContext context, List<PriceRule> activeRules);
    decimal CalculateUnitPrice(PricingContext context, List<PriceRule> activeRules);
}