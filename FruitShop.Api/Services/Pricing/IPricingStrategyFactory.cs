using FruitShop.Api.Models;
using FruitShop.Api.Services.ConditionEvaluator;
using FruitShop.Api.Services.PriceActionStrategy;

namespace FruitShop.Api.Services;

public interface IPricingStrategyFactory
{
    IConditionEvaluator GetConditionEvaluator(string attribute);

    IPriceActionStrategy GetActionStrategy(ActionType actionType);
}