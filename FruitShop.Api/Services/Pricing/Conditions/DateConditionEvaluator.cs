using FruitShop.Api.Models;

namespace FruitShop.Api.Services.ConditionEvaluator;

public class DateConditionEvaluator : IConditionEvaluator
{
    public string HandlesAttribute => "orderDate";

    public bool IsSatisfied(PriceRuleCondition condition, PricingContext context)
    {
        if (condition.Operator == "BETWEEN")
        {
            var (startDate, endDate) = PriceRuleConditionSchema.ParseDateRange(condition.Value);
            
            return context.OrderDate >= startDate && context.OrderDate <= endDate;
        }

        var value = PriceRuleConditionSchema.ParseDate(condition.Value);

        return condition.Operator switch
        {
            ">=" or "GreaterThanOrEqual" => context.OrderDate >= value,
            ">" or "GreaterThan" => context.OrderDate > value,
            "<=" or "LessThanOrEqual" => context.OrderDate <= value,
            "<" or "LessThan" => context.OrderDate < value,
            "=" or "Equal" => context.OrderDate == value,
            _ => throw new NotSupportedException($"Operator {condition.Operator} not supported for order date.")
        };
    }
}