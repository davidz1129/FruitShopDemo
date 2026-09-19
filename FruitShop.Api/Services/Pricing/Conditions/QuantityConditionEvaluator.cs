using FruitShop.Api.Models;

namespace FruitShop.Api.Services.ConditionEvaluator;

public class QuantityConditionEvaluator : IConditionEvaluator
{
    public string HandlesAttribute => "quantity";

    public bool IsSatisfied(PriceRuleCondition condition, PricingContext context)
    {
        var threshold = PriceRuleConditionSchema.ParseDecimal(condition.Value);
        
        return condition.Operator switch
        {
            ">=" or "GreaterThanOrEqual" => context.Quantity >= threshold,
            ">" or "GreaterThan" => context.Quantity > threshold,
            "<=" or "LessThanOrEqual" => context.Quantity <= threshold,
            "<" or "LessThan" => context.Quantity < threshold,
            "=" or "Equal" => context.Quantity == threshold,
            _ => throw new NotSupportedException($"Operator {condition.Operator} not supported for quantity.")
        };
    }
}

