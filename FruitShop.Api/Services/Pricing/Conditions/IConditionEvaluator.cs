using FruitShop.Api.Models;

namespace FruitShop.Api.Services.ConditionEvaluator;

public interface IConditionEvaluator
{
    // The string matches the 'Attribute' column in the DB (e.g., "quantity", "date")
    string HandlesAttribute { get; } 
    bool IsSatisfied(PriceRuleCondition condition, PricingContext context);
}
