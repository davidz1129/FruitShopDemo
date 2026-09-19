using FruitShop.Api.Models;
using System.Globalization;

namespace FruitShop.Api.Services.PriceActionStrategy;

public class AmountOffStrategy : IPriceActionStrategy
{
    public ActionType HandlesAction => ActionType.AmountOff;

    public decimal Apply(PriceRuleAction action, decimal runningPrice, decimal originalBasePrice)
    {
        var targetBase = action.CalculationBase == CalculationBase.OriginalBase
            ? originalBasePrice
            : runningPrice;

        return targetBase - action.Amount;
    }

    public string Describe(PriceRuleAction action) =>
        $"${action.Amount.ToString("0.####", CultureInfo.InvariantCulture)} off";
}