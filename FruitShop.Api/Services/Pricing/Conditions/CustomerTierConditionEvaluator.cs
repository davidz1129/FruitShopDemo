using FruitShop.Api.Models;

namespace FruitShop.Api.Services.ConditionEvaluator;

public class CustomerTierConditionEvaluator : IConditionEvaluator
{
    public string HandlesAttribute => "customerTier";

    public bool IsSatisfied(PriceRuleCondition condition, PricingContext context) =>
        condition.Operator switch
        {
            "=" => context.CustomerTier.Equals(condition.Value, StringComparison.OrdinalIgnoreCase),
            _ => throw new NotSupportedException($"Operator {condition.Operator} not supported for customer tier.")
        };
}