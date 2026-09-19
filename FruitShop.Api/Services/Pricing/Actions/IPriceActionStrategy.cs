using FruitShop.Api.Models;

namespace FruitShop.Api.Services.PriceActionStrategy;

public interface IPriceActionStrategy
{
    ActionType HandlesAction { get; }
    decimal Apply(PriceRuleAction action, decimal runningPrice, decimal originalBasePrice);
    decimal Apply(PriceRuleAction action, decimal runningUnitPrice, PricingContext context) =>
        Apply(action, runningUnitPrice, context.OriginalBasePrice);
    string Describe(PriceRuleAction action);
}