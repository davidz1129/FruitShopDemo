using FruitShop.Api.Models;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;

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

    public decimal Apply(PriceRuleAction action, decimal runningUnitPrice, PricingContext context)
    {
        if (action.CalculationBase == CalculationBase.OriginalBase)
        {
            return Apply(action, runningUnitPrice, context.OriginalBasePrice);
        }

        if (context.Quantity <= 0)
        {
            return Apply(action, runningUnitPrice, context.OriginalBasePrice);
        }

        decimal runningLineTotal = runningUnitPrice * context.Quantity;
        return (runningLineTotal - action.Amount) / context.Quantity;
    }

    public string Describe(PriceRuleAction action) =>
        $"${action.Amount.ToString("0.####", CultureInfo.InvariantCulture)} off ({GetDescription(action.CalculationBase)})";

    private static string GetDescription(CalculationBase calculationBase) =>
        calculationBase.GetType()
            .GetField(calculationBase.ToString())?
            .GetCustomAttribute<DescriptionAttribute>()?
            .Description
        ?? calculationBase.ToString();
}