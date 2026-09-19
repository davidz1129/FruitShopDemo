using FruitShop.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FruitShop.Api.Data;

public static class FruitShopDemoSeeder
{
    public static async Task SeedAsync(FruitShopDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await SeedUnitsOfMeasureAsync(dbContext, cancellationToken);
        await SeedProductsAsync(dbContext, cancellationToken);
        await SeedPriceRulesAsync(dbContext, cancellationToken);
    }

    private static async Task SeedUnitsOfMeasureAsync(FruitShopDbContext dbContext, CancellationToken cancellationToken)
    {
        var unitsByCode = await dbContext.UnitsOfMeasure
            .ToDictionaryAsync(unit => unit.Code, cancellationToken);

        foreach (var unit in GetUnitsOfMeasure())
        {
            if (!unitsByCode.ContainsKey(unit.Code))
            {
                dbContext.UnitsOfMeasure.Add(unit);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedProductsAsync(FruitShopDbContext dbContext, CancellationToken cancellationToken)
    {
        var existingSkus = await dbContext.ProductVariants
            .Select(variant => variant.Sku)
            .ToHashSetAsync(cancellationToken);

        foreach (var product in GetProducts())
        {
            var missingVariants = product.Variants
                .Where(variant => !existingSkus.Contains(variant.Sku))
                .Select(CloneVariant)
                .ToList();

            if (missingVariants.Count == 0)
            {
                continue;
            }

            dbContext.Products.Add(new Product
            {
                Name = product.Name,
                Variants = missingVariants
            });

            foreach (var variant in missingVariants)
            {
                existingSkus.Add(variant.Sku);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedPriceRulesAsync(FruitShopDbContext dbContext, CancellationToken cancellationToken)
    {
        var variantsBySku = await dbContext.ProductVariants
            .ToDictionaryAsync(variant => variant.Sku, cancellationToken);

        var existingRuleNames = await dbContext.PriceRules
            .Select(rule => rule.Name)
            .ToHashSetAsync(cancellationToken);

        foreach (var definition in GetPriceRules())
        {
            if (existingRuleNames.Contains(definition.Name))
            {
                continue;
            }

            var rule = new PriceRule
            {
                Name = definition.Name,
                Priority = definition.Priority,
                IsStackable = definition.IsStackable,
                Status = RuleStatus.Active,
                Conditions = definition.Conditions
                    .Select(condition => new PriceRuleCondition
                    {
                        Attribute = condition.Attribute,
                        Operator = condition.Operator,
                        Value = condition.Value
                    })
                    .ToList(),
                Actions = definition.Actions
                    .Select(action => new PriceRuleAction
                    {
                        ActionType = action.ActionType,
                        Amount = action.Amount,
                        CalculationBase = action.CalculationBase
                    })
                    .ToList(),
                TargetCustomerTiers = definition.CustomerTiers
                    .Select(customerTier => new RuleTargetCustomerTier
                    {
                        CustomerTier = customerTier
                    })
                    .ToList(),
                TargetVariants = definition.TargetSkus
                    .Select(sku => new RuleTargetVariant
                    {
                        VariantId = variantsBySku[sku].Id
                    })
                    .ToList()
            };

            dbContext.PriceRules.Add(rule);
            existingRuleNames.Add(rule.Name);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static IEnumerable<UnitOfMeasure> GetUnitsOfMeasure()
    {
        yield return new UnitOfMeasure { Code = "kg", MeasureType = MeasureType.Weight };
        yield return new UnitOfMeasure { Code = "each", MeasureType = MeasureType.Count };
        yield return new UnitOfMeasure { Code = "bottle", MeasureType = MeasureType.Count };
        yield return new UnitOfMeasure { Code = "box", MeasureType = MeasureType.Count };
    }

    private static IEnumerable<ProductSeed> GetProducts()
    {
        yield return new ProductSeed(
            "Honeycrisp Apples",
            [
                new ProductVariant
                {
                    Sku = "APPLE-HONEY-1KG",
                    UomCode = "kg",
                    UomFactor = 1.0000m,
                    BasePrice = 4.9900m
                }
            ]);

        yield return new ProductSeed(
            "Cavendish Bananas",
            [
                new ProductVariant
                {
                    Sku = "BANANA-CAV-1KG",
                    UomCode = "kg",
                    UomFactor = 1.0000m,
                    BasePrice = 2.4900m
                }
            ]);

        yield return new ProductSeed(
            "Navel Oranges",
            [
                new ProductVariant
                {
                    Sku = "ORANGE-NAVEL-1KG",
                    UomCode = "kg",
                    UomFactor = 1.0000m,
                    BasePrice = 3.7900m
                }
            ]);

        yield return new ProductSeed(
            "Hass Avocados",
            [
                new ProductVariant
                {
                    Sku = "AVOCADO-HASS-EACH",
                    UomCode = "each",
                    UomFactor = 1.0000m,
                    BasePrice = 1.6900m
                }
            ]);

        yield return new ProductSeed(
            "Fresh Orange Juice",
            [
                new ProductVariant
                {
                    Sku = "JUICE-ORANGE-1L",
                    UomCode = "bottle",
                    UomFactor = 1.0000m,
                    BasePrice = 5.4900m
                }
            ]);

        yield return new ProductSeed(
            "Kiwi",
            [
                new ProductVariant
                {
                    Sku = "KIWI-1KG",
                    UomCode = "kg",
                    UomFactor = 1.0000m,
                    BasePrice = 9.9900m
                },
                new ProductVariant
                {
                    Sku = "KIWI-4-PACK",
                    UomCode = "box",
                    UomFactor = 4.0000m,
                    BasePrice = 9.0000m
                }
            ]);

        yield return new ProductSeed(
            "Cherry",
            [
                new ProductVariant
                {
                    Sku = "CHERRY-1KG",
                    UomCode = "kg",
                    UomFactor = 1.0000m,
                    BasePrice = 5.0000m
                },
                new ProductVariant
                {
                    Sku = "CHERRY-5KG-BOX",
                    UomCode = "box",
                    UomFactor = 1.0000m,
                    BasePrice = 45.0000m
                }
            ]);
    }

    private static IEnumerable<PriceRuleSeed> GetPriceRules()
    {
        yield return new PriceRuleSeed(
            "Retail 10% Quantity Discount (3+)",
            Priority: 10,
            IsStackable: true,
            CustomerTiers: ["RETAIL"],
            TargetSkus: ["ORANGE-NAVEL-1KG", "JUICE-ORANGE-1L"],
            Conditions:
            [
                new RuleConditionSeed("Quantity", ">=", "3"),
                new RuleConditionSeed("CustomerTier", "=", "RETAIL")
            ],
            Actions:
            [
                new RuleActionSeed(ActionType.PercentageDiscount, 10.0000m, CalculationBase.RunningTotal)
            ]);

        yield return new PriceRuleSeed(
            "VIP 5% Discount",
            Priority: 1,
            IsStackable: true,
            CustomerTiers: ["VIP"],
            TargetSkus:
            [
                "APPLE-HONEY-1KG",
                "BANANA-CAV-1KG",
                "ORANGE-NAVEL-1KG",
                "AVOCADO-HASS-EACH",
                "JUICE-ORANGE-1L",
                "CHERRY-1KG",
                "CHERRY-5KG-BOX"
            ],
            Conditions:
            [
                new RuleConditionSeed("CustomerTier", "=", "VIP")
            ],
            Actions:
            [
                new RuleActionSeed(ActionType.PercentageDiscount, 5.0000m, CalculationBase.RunningTotal)
            ]);

        yield return new PriceRuleSeed(
            "Wholesale $1.25 Quantity Price Override (10+)",
            Priority: 5,
            IsStackable: false,
            CustomerTiers: ["WHOLESALE"],
            TargetSkus: ["AVOCADO-HASS-EACH"],
            Conditions:
            [
                new RuleConditionSeed("Quantity", ">=", "10"),
                new RuleConditionSeed("CustomerTier", "=", "WHOLESALE")
            ],
            Actions:
            [
                new RuleActionSeed(ActionType.OverridePrice, 1.2500m, CalculationBase.OriginalBase)
            ]);

        yield return new PriceRuleSeed(
            "Autumn $2.99 Price Override",
            Priority: 8,
            IsStackable: true,
            CustomerTiers: [],
            TargetSkus: ["APPLE-HONEY-1KG"],
            Conditions:
            [
                new RuleConditionSeed("Date", "BETWEEN", "2026-09-01 AND 2026-11-15")
            ],
            Actions:
            [
                new RuleActionSeed(ActionType.OverridePrice, 2.9900m, CalculationBase.OriginalBase)
            ]);

        yield return new PriceRuleSeed(
            "10% Quantity Discount (5+)",
            Priority: 7,
            IsStackable: true,
            CustomerTiers: [],
            TargetSkus: ["APPLE-HONEY-1KG"],
            Conditions:
            [
                new RuleConditionSeed("Quantity", ">=", "5")
            ],
            Actions:
            [
                new RuleActionSeed(ActionType.PercentageDiscount, 10.0000m, CalculationBase.RunningTotal)
            ]);

        yield return new PriceRuleSeed(
            "10% Quantity Discount (2+)",
            Priority: 15,
            IsStackable: true,
            CustomerTiers: [],
            TargetSkus: ["CHERRY-1KG"],
            Conditions:
            [
                new RuleConditionSeed("Quantity", ">=", "2")
            ],
            Actions:
            [
                new RuleActionSeed(ActionType.PercentageDiscount, 10.0000m, CalculationBase.RunningTotal)
            ]);
    }

    private static ProductVariant CloneVariant(ProductVariant variant) =>
        new()
        {
            Sku = variant.Sku,
            UomCode = variant.UomCode,
            UomFactor = variant.UomFactor,
            BasePrice = variant.BasePrice
        };

    private sealed record ProductSeed(string Name, IReadOnlyList<ProductVariant> Variants);

    private sealed record PriceRuleSeed(
        string Name,
        int Priority,
        bool IsStackable,
        IReadOnlyList<string> CustomerTiers,
        IReadOnlyList<string> TargetSkus,
        IReadOnlyList<RuleConditionSeed> Conditions,
        IReadOnlyList<RuleActionSeed> Actions);

    private sealed record RuleConditionSeed(string Attribute, string Operator, string Value);

    private sealed record RuleActionSeed(ActionType ActionType, decimal Amount, CalculationBase CalculationBase);
}