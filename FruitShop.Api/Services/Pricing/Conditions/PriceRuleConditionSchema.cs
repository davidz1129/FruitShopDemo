using System.Globalization;

namespace FruitShop.Api.Services.ConditionEvaluator;

public static class PriceRuleConditionSchema
{
    private const string DateFormat = "yyyy-MM-dd";
    private const string DateRangeSeparator = " AND ";

    public static bool IsSupportedAttribute(string? attribute) =>
        attribute is not null && NormalizeAttribute(attribute) is "quantity" or "cartsubtotal" or "customertier" or "orderdate";

    public static bool IsSupportedOperator(string? attribute, string? @operator)
    {
        if (!IsSupportedAttribute(attribute) || string.IsNullOrWhiteSpace(@operator))
        {
            return false;
        }

        return NormalizeAttribute(attribute!) switch
        {
            "customertier" => @operator.Trim() == "=",
            "orderdate" => IsComparisonOperator(@operator) || @operator.Trim() == "BETWEEN",
            _ => IsComparisonOperator(@operator)
        };
    }

    public static bool HasValidValue(string? attribute, string? @operator, string? value)
    {
        if (!IsSupportedOperator(attribute, @operator) || string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return NormalizeAttribute(attribute!) switch
        {
            "quantity" or "cartsubtotal" => TryParseDecimal(value, out _),
            "customertier" => IsSupportedCustomerTier(value),
            "orderdate" when @operator!.Trim() == "BETWEEN" => TryParseDateRange(value, out _, out _),
            "orderdate" => TryParseDate(value, out _),
            _ => false
        };
    }

    public static string NormalizeAttribute(string attribute) =>
        attribute.Trim().ToLowerInvariant() switch
        {
            "date" => "orderdate",
            var normalized => normalized
        };

    public static decimal ParseDecimal(string value) =>
        TryParseDecimal(value, out var parsed)
            ? parsed
            : throw new FormatException("Price rule numeric condition values must use invariant decimal notation.");

    public static DateTime ParseDate(string value) =>
        TryParseDate(value, out var parsed)
            ? parsed
            : throw new FormatException($"Price rule dates must use the {DateFormat} format.");

    public static (DateTime Start, DateTime End) ParseDateRange(string value) =>
        TryParseDateRange(value, out var start, out var end)
            ? (start, end)
            : throw new FormatException($"Price rule date ranges must use {DateFormat}{DateRangeSeparator}{DateFormat} with the start date first.");

    private static bool IsSupportedCustomerTier(string value) =>
        new[] { "RETAIL", "VIP", "WHOLESALE" }.Contains(value.Trim(), StringComparer.OrdinalIgnoreCase);

    private static bool IsComparisonOperator(string @operator) =>
        @operator.Trim() is ">=" or ">" or "<=" or "<" or "=" or "GreaterThanOrEqual" or "GreaterThan" or "LessThanOrEqual" or "LessThan" or "Equal";

    private static bool TryParseDecimal(string value, out decimal parsed) =>
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out parsed);

    private static bool TryParseDate(string value, out DateTime parsed) =>
        DateTime.TryParseExact(value.Trim(), DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed);

    private static bool TryParseDateRange(string value, out DateTime start, out DateTime end)
    {
        start = default;
        end = default;
        var parts = value.Split(DateRangeSeparator, StringSplitOptions.None);
        return parts.Length == 2
            && TryParseDate(parts[0], out start)
            && TryParseDate(parts[1], out end)
            && start <= end;
    }
}