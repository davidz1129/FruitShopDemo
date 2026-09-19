using FruitShop.Api.Models;

namespace FruitShop.Api.Services.PriceActionStrategy;

public interface IPriceActionStrategy
{
    ActionType HandlesAction { get; }
    decimal Apply(PriceRuleAction action, decimal runningPrice, decimal originalBasePrice);
    string Describe(PriceRuleAction action);
}