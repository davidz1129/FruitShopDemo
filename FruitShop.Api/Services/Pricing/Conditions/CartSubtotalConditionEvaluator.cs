using FruitShop.Api.Models;

namespace FruitShop.Api.Services.ConditionEvaluator;

public class CartSubtotalConditionEvaluator : IConditionEvaluator
{
    public string HandlesAttribute => "cartSubtotal";

    public bool IsSatisfied(PriceRuleCondition condition, PricingContext context)
    {
        var threshold = PriceRuleConditionSchema.ParseDecimal(condition.Value);

        return condition.Operator switch
        {
            ">=" or "GreaterThanOrEqual" => context.CartSubtotal >= threshold,
            ">" or "GreaterThan" => context.CartSubtotal > threshold,
            "<=" or "LessThanOrEqual" => context.CartSubtotal <= threshold,
            "<" or "LessThan" => context.CartSubtotal < threshold,
            "=" or "Equal" => context.CartSubtotal == threshold,
            _ => throw new NotSupportedException($"Operator {condition.Operator} not supported for cart subtotal.")
        };
    }
}