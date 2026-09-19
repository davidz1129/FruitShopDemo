using FruitShop.Api.Models;
using FruitShop.Api.Services.ConditionEvaluator;
using FruitShop.Api.Services.PriceActionStrategy;

namespace FruitShop.Api.Services;

public class PricingStrategyFactory : IPricingStrategyFactory
{
    private readonly IEnumerable<IConditionEvaluator> _conditionEvaluators;
    private readonly IEnumerable<IPriceActionStrategy> _actionStrategies;

    public PricingStrategyFactory(IEnumerable<IConditionEvaluator> conditionEvaluators, IEnumerable<IPriceActionStrategy> actionStrategies)
    {
        _conditionEvaluators = conditionEvaluators;
        _actionStrategies = actionStrategies;
    }

    public IConditionEvaluator GetConditionEvaluator(string attribute) =>
        _conditionEvaluators.First(e => NormalizeAttribute(e.HandlesAttribute) == NormalizeAttribute(attribute));

    public IPriceActionStrategy GetActionStrategy(ActionType actionType) =>
        _actionStrategies.First(s => s.HandlesAction == actionType);

    private static string NormalizeAttribute(string attribute) =>
        PriceRuleConditionSchema.NormalizeAttribute(attribute);
}