using FruitShop.Api.Models;
using System.Globalization;

namespace FruitShop.Api.Services.PriceActionStrategy;

public class PercentageDiscountStrategy : IPriceActionStrategy
{
    public ActionType HandlesAction => ActionType.PercentageDiscount;

    public decimal Apply(PriceRuleAction action, decimal runningPrice, decimal originalBasePrice)
    {
        // Check if the 15% off applies to the original $10 or the currently discounted $8
        var targetBase = action.CalculationBase == CalculationBase.OriginalBase 
            ? originalBasePrice 
            : runningPrice;

        var discountAmount = targetBase * (action.Amount / 100m);
        return Math.Round(runningPrice - discountAmount, 2, MidpointRounding.AwayFromZero);
    }

    public string Describe(PriceRuleAction action) =>
        $"{action.Amount.ToString("0.####", CultureInfo.InvariantCulture)}% off";
}