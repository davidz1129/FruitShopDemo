using FruitShop.Api.Models;
using System.Globalization;

namespace FruitShop.Api.Services;

public class PricingService : IPricingService
{
    private readonly IPricingStrategyFactory _factory;

    public PricingService(IPricingStrategyFactory factory)
    {
        _factory = factory;
    }

    public decimal CalculateUnitPrice(PricingContext context, List<PriceRule> activeRules) =>
        CalculatePrice(context, activeRules).UnitPriceApplied;

    public PriceCalculation CalculatePrice(PricingContext context, List<PriceRule> activeRules)
    {
        decimal runningTotal = context.OriginalBasePrice;
        var appliedRuleDescriptions = new List<string>();

        // Rules should already be fetched from the DB ordered by Priority DESC
        foreach (var rule in activeRules)
        {
            if (rule.Status != RuleStatus.Active)
            {
                continue;
            }

            // 1. Evaluate all conditions (AND logic)
            bool allConditionsMet = true;
            foreach (var condition in rule.Conditions)
            {
                var evaluator = _factory.GetConditionEvaluator(condition.Attribute);
                if (!evaluator.IsSatisfied(condition, context))
                {
                    allConditionsMet = false;
                    break; 
                }
            }

            // 2. If conditions are met, apply the action strategies
            if (allConditionsMet)
            {
                decimal priceBeforeRule = runningTotal;
                var actionDescriptions = new List<string>();

                foreach (var action in rule.Actions)
                {
                    var strategy = _factory.GetActionStrategy(action.ActionType);
                    runningTotal = strategy.Apply(action, runningTotal, context);
                    actionDescriptions.Add(strategy.Describe(action));
                }

                if (runningTotal != priceBeforeRule)
                {
                    appliedRuleDescriptions.Add($"{rule.Name} ({string.Join(", ", actionDescriptions)})");
                }

                // 3. Pipeline Break: Check stackability
                if (!rule.IsStackable)
                {
                    break; // Stop evaluating further rules
                }
            }
        }

        decimal unitPriceApplied = Math.Max(0, runningTotal);
        string basePrice = context.OriginalBasePrice.ToString("0.00", CultureInfo.InvariantCulture);
        string priceChangeReason = appliedRuleDescriptions.Count == 0
            ? $"Base price ${basePrice} applied"
            : $"Base price ${basePrice}; adjusted by: {string.Join("; ", appliedRuleDescriptions)}";

        return new PriceCalculation(unitPriceApplied, priceChangeReason);
    }
}