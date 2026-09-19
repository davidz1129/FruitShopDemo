using FruitShop.Api.Models;
using System.Globalization;

namespace FruitShop.Api.Services.PriceActionStrategy;

public class OverridePriceStrategy : IPriceActionStrategy
{
    public ActionType HandlesAction => ActionType.OverridePrice;

    public decimal Apply(PriceRuleAction action, decimal runningPrice, decimal originalBasePrice)
    {
        return action.Amount; // Completely replaces the price (e.g., seasonal fixed price)
    }

    public string Describe(PriceRuleAction action) =>
        $"Override price to ${action.Amount.ToString("0.####", CultureInfo.InvariantCulture)}";
}