using FruitShop.Api.Data;
using FruitShop.Api.Models;
using FruitShop.Api.Repositories;
using FruitShop.Api.Services;
using FruitShop.Api.Services.ConditionEvaluator;
using FruitShop.Api.Services.PriceActionStrategy;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using System.Globalization;
using System.Text.Json;

namespace FruitShop.Api.Tests;

public sealed class CoreDomainTests
{
    [Fact]
    public void UnitOfMeasure_serializes_measure_type_as_a_string()
    {
        var unit = new UnitOfMeasure { Code = "kg", MeasureType = MeasureType.Weight };

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(unit, new JsonSerializerOptions(JsonSerializerDefaults.Web)));

        Assert.Equal("Weight", document.RootElement.GetProperty("measureType").GetString());
    }

    [Theory]
    [InlineData(CalculationBase.RunningTotal, 8, 10, 3, 5)]
    [InlineData(CalculationBase.OriginalBase, 8, 10, 3, 7)]
    public void AmountOffStrategy_applies_discount_from_selected_base(CalculationBase calculationBase, decimal runningPrice, decimal originalBasePrice, decimal amountOff, decimal expected)
    {
        var strategy = new AmountOffStrategy();
        var action = new PriceRuleAction
        {
            ActionType = ActionType.AmountOff,
            Amount = amountOff,
            CalculationBase = calculationBase
        };

        Assert.Equal(expected, strategy.Apply(action, runningPrice, originalBasePrice));
    }

    [Fact]
    public void PercentageDiscountStrategy_rounds_price_to_two_decimal_places()
    {
        var strategy = new PercentageDiscountStrategy();
        var action = new PriceRuleAction
        {
            ActionType = ActionType.PercentageDiscount,
            Amount = 10,
            CalculationBase = CalculationBase.RunningTotal
        };

        var discountedPrice = strategy.Apply(action, 10.05m, 10.05m);

        Assert.Equal(9.05m, discountedPrice);
    }

    [Fact]
    public void PricingService_applies_amount_off_rules()
    {
        var factory = new PricingStrategyFactory(
            [new CustomerTierConditionEvaluator()],
            [new PercentageDiscountStrategy(), new AmountOffStrategy(), new OverridePriceStrategy()]);
        var service = new PricingService(factory);
        var context = new PricingContext(0, "VIP", 0, default, 10, 25);
        var rule = new PriceRule
        {
            Name = "VIP Basket Discount",
            Priority = 20,
            IsStackable = true,
            Conditions =
            [
                new PriceRuleCondition { Attribute = "CustomerTier", Operator = "=", Value = "VIP" }
            ],
            Actions =
            [
                new PriceRuleAction { ActionType = ActionType.AmountOff, Amount = 3, CalculationBase = CalculationBase.RunningTotal }
            ]
        };

        Assert.Equal(7, service.CalculateUnitPrice(context, [rule]));

    var calculation = service.CalculatePrice(context, [rule]);

    Assert.Equal(7, calculation.UnitPriceApplied);
    Assert.Equal("Base price $10.00; adjusted by: VIP Basket Discount ($3 off)", calculation.PriceChangeReason);
    }

    [Fact]
    public void PricingService_includes_human_readable_override_and_percentage_actions_in_reason()
    {
        var factory = new PricingStrategyFactory(
            [],
            [new PercentageDiscountStrategy(), new AmountOffStrategy(), new OverridePriceStrategy()]);
        var service = new PricingService(factory);
        var context = new PricingContext(0, "RETAIL", 1, default, 10);
        var fixedPriceRule = new PriceRule
        {
            Name = "Season Sale",
            Priority = 20,
            IsStackable = true,
            Actions =
            [
                new PriceRuleAction { ActionType = ActionType.OverridePrice, Amount = 8 }
            ]
        };
        var percentageRule = new PriceRule
        {
            Name = "Member Discount",
            Priority = 10,
            IsStackable = true,
            Actions =
            [
                new PriceRuleAction { ActionType = ActionType.PercentageDiscount, Amount = 10 }
            ]
        };

        var calculation = service.CalculatePrice(context, [fixedPriceRule, percentageRule]);

        Assert.Equal(7.2m, calculation.UnitPriceApplied);
        Assert.Equal(
            "Base price $10.00; adjusted by: Season Sale (Override price to $8); Member Discount (10% off)",
            calculation.PriceChangeReason);
    }

    [Fact]
    public void PricingService_ignores_inactive_rules()
    {
        var factory = new PricingStrategyFactory(
            [new CustomerTierConditionEvaluator()],
            [new PercentageDiscountStrategy(), new AmountOffStrategy(), new OverridePriceStrategy()]);
        var service = new PricingService(factory);
        var context = new PricingContext(0, "VIP", 0, default, 10, 25);
        var inactiveRule = new PriceRule
        {
            Name = "Inactive VIP Discount",
            Priority = 20,
            IsStackable = true,
            Status = RuleStatus.Inactive,
            Conditions =
            [
                new PriceRuleCondition { Attribute = "CustomerTier", Operator = "=", Value = "VIP" }
            ],
            Actions =
            [
                new PriceRuleAction { ActionType = ActionType.AmountOff, Amount = 3, CalculationBase = CalculationBase.RunningTotal }
            ]
        };

        Assert.Equal(10, service.CalculateUnitPrice(context, [inactiveRule]));
        Assert.Equal("Base price $10.00 applied", service.CalculatePrice(context, [inactiveRule]).PriceChangeReason);
    }

    [Theory]
    [InlineData(">=", 24.99, false)]
    [InlineData(">=", 25.00, true)]
    [InlineData(">", 25.00, false)]
    [InlineData(">", 25.01, true)]
    [InlineData("<=", 25.00, true)]
    [InlineData("<=", 25.01, false)]
    [InlineData("<", 25.00, false)]
    [InlineData("<", 24.99, true)]
    [InlineData("=", 25.00, true)]
    [InlineData("=", 24.99, false)]
    public void CartSubtotalConditionEvaluator_evaluates_numeric_operators(string @operator, decimal cartSubtotal, bool expected)
    {
        var evaluator = new CartSubtotalConditionEvaluator();
        var condition = new PriceRuleCondition { Attribute = "CartSubtotal", Operator = @operator, Value = "25" };
        var context = new PricingContext(0, "VIP", 0, default, 0, cartSubtotal);

        Assert.Equal(expected, evaluator.IsSatisfied(condition, context));
    }

    [Fact]
    public void CartSubtotalConditionEvaluator_rejects_unsupported_operators()
    {
        var evaluator = new CartSubtotalConditionEvaluator();
        var condition = new PriceRuleCondition { Attribute = "CartSubtotal", Operator = "BETWEEN", Value = "25" };
        var context = new PricingContext(0, "VIP", 0, default, 0, 25);

        Assert.Throws<NotSupportedException>(() => evaluator.IsSatisfied(condition, context));
    }

    [Fact]
    public void Numeric_condition_evaluators_use_invariant_decimal_values()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");

            var quantityCondition = new PriceRuleCondition { Attribute = "Quantity", Operator = ">=", Value = "3.5" };
            var subtotalCondition = new PriceRuleCondition { Attribute = "CartSubtotal", Operator = ">=", Value = "25.50" };
            var context = new PricingContext(0, "VIP", 4, default, 0, 26);

            Assert.True(new QuantityConditionEvaluator().IsSatisfied(quantityCondition, context));
            Assert.True(new CartSubtotalConditionEvaluator().IsSatisfied(subtotalCondition, context));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [Theory]
    [InlineData("VIP", true)]
    [InlineData("retail", false)]
    public void CustomerTierConditionEvaluator_evaluates_equality(string customerTier, bool expected)
    {
        var evaluator = new CustomerTierConditionEvaluator();
        var condition = new PriceRuleCondition { Attribute = "CustomerTier", Operator = "=", Value = "VIP" };
        var context = new PricingContext(0, customerTier, 0, default, 0);

        Assert.Equal(expected, evaluator.IsSatisfied(condition, context));
    }

    [Fact]
    public void CustomerTierConditionEvaluator_rejects_unsupported_operators()
    {
        var evaluator = new CustomerTierConditionEvaluator();
        var condition = new PriceRuleCondition { Attribute = "CustomerTier", Operator = ">=", Value = "VIP" };
        var context = new PricingContext(0, "VIP", 0, default, 0);

        Assert.Throws<NotSupportedException>(() => evaluator.IsSatisfied(condition, context));
    }

    [Theory]
    [InlineData("GreaterThanOrEqual", "2026-09-01", "2026-09-18", true)]
    [InlineData("LessThanOrEqual", "2026-10-15", "2026-09-18", true)]
    [InlineData("GreaterThan", "2026-09-18", "2026-09-18", false)]
    public void DateConditionEvaluator_supports_canonical_comparison_operators(string @operator, string value, string orderDate, bool expected)
    {
        var evaluator = new DateConditionEvaluator();
        var condition = new PriceRuleCondition { Attribute = "OrderDate", Operator = @operator, Value = value };
        var context = new PricingContext(0, "VIP", 0, DateTime.Parse(orderDate), 0);

        Assert.Equal(expected, evaluator.IsSatisfied(condition, context));
    }

    [Fact]
    public async Task SeedAsync_adds_demo_catalog_and_rules_once()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<FruitShopDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new FruitShopDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        await FruitShopDemoSeeder.SeedAsync(dbContext);
        await FruitShopDemoSeeder.SeedAsync(dbContext);

        Assert.Equal(4, await dbContext.UnitsOfMeasure.CountAsync());
        Assert.Equal(7, await dbContext.Products.CountAsync());
        Assert.Equal(9, await dbContext.ProductVariants.CountAsync());
        Assert.Equal(6, await dbContext.PriceRules.CountAsync());
        Assert.Equal(8, await dbContext.PriceRuleConditions.CountAsync());
        Assert.Equal(6, await dbContext.PriceRuleActions.CountAsync());
        Assert.Equal(3, await dbContext.RuleTargetCustomerTiers.CountAsync());
        Assert.Equal(13, await dbContext.RuleTargetVariants.CountAsync());

        var appleVariant = await dbContext.ProductVariants
            .SingleAsync(variant => variant.Sku == "APPLE-HONEY-1KG");

        var cherryVariant = await dbContext.ProductVariants
            .SingleAsync(variant => variant.Sku == "CHERRY-1KG");

        var kiwiVariants = await dbContext.Products
            .Where(product => product.Name == "Kiwi")
            .SelectMany(product => product.Variants)
            .ToListAsync();

        Assert.Equal("kg", appleVariant.UomCode);
        Assert.Equal(4.9900m, appleVariant.BasePrice);
        Assert.Equal("kg", cherryVariant.UomCode);
        Assert.Equal(5.0000m, cherryVariant.BasePrice);
        Assert.Collection(
            kiwiVariants.OrderBy(variant => variant.Sku),
            kilogramVariant =>
            {
                Assert.Equal("KIWI-1KG", kilogramVariant.Sku);
                Assert.Equal("kg", kilogramVariant.UomCode);
                Assert.Equal(1.0000m, kilogramVariant.UomFactor);
                Assert.Equal(9.9900m, kilogramVariant.BasePrice);
            },
            fourPackVariant =>
            {
                Assert.Equal("KIWI-4-PACK", fourPackVariant.Sku);
                Assert.Equal("box", fourPackVariant.UomCode);
                Assert.Equal(4.0000m, fourPackVariant.UomFactor);
                Assert.Equal(9.0000m, fourPackVariant.BasePrice);
            });

        var citrusRule = await dbContext.PriceRules
            .Include(rule => rule.Actions)
            .Include(rule => rule.Conditions)
            .Include(rule => rule.TargetVariants)
            .SingleAsync(rule => rule.Name == "Retail 10% Quantity Discount (3+)");

        Assert.True(citrusRule.IsStackable);
        Assert.Equal(10, citrusRule.Priority);
        Assert.Single(citrusRule.Actions, action => action.ActionType == Models.ActionType.PercentageDiscount && action.Amount == 10.0000m);
        Assert.Contains(citrusRule.Conditions, condition => condition.Attribute == "Quantity" && condition.Operator == ">=" && condition.Value == "3");
        Assert.Equal(2, citrusRule.TargetVariants.Count);

        var vipRule = await dbContext.PriceRules
            .Include(rule => rule.Actions)
            .Include(rule => rule.Conditions)
            .Include(rule => rule.TargetVariants)
            .Include(rule => rule.TargetCustomerTiers)
            .SingleAsync(rule => rule.Name == "VIP 5% Discount");

        Assert.True(vipRule.IsStackable);
        Assert.Equal(1, vipRule.Priority);
        Assert.Single(vipRule.Actions, action => action.ActionType == Models.ActionType.PercentageDiscount && action.Amount == 5.0000m);
        Assert.Contains(vipRule.Conditions, condition => condition.Attribute == "CustomerTier" && condition.Operator == "=" && condition.Value == "VIP");
        Assert.Equal(7, vipRule.TargetVariants.Count);
        Assert.Single(vipRule.TargetCustomerTiers, target => target.CustomerTier == "VIP");

        var honeycrispSeasonSaleRule = await dbContext.PriceRules
            .Include(rule => rule.Actions)
            .Include(rule => rule.Conditions)
            .Include(rule => rule.TargetVariants)
            .SingleAsync(rule => rule.Name == "Autumn $2.99 Price Override");

        Assert.True(honeycrispSeasonSaleRule.IsStackable);
        Assert.Equal(8, honeycrispSeasonSaleRule.Priority);
        Assert.Single(honeycrispSeasonSaleRule.Actions, action => action.ActionType == Models.ActionType.OverridePrice && action.Amount == 2.9900m && action.CalculationBase == Models.CalculationBase.OriginalBase);
        Assert.Single(honeycrispSeasonSaleRule.Conditions, condition => condition.Attribute == "Date" && condition.Operator == "BETWEEN" && condition.Value == "2026-09-01 AND 2026-11-15");
        Assert.Single(honeycrispSeasonSaleRule.TargetVariants, target => target.VariantId == appleVariant.Id);

        var honeycrispVolumeDiscountRule = await dbContext.PriceRules
            .Include(rule => rule.Actions)
            .Include(rule => rule.Conditions)
            .Include(rule => rule.TargetVariants)
            .SingleAsync(rule => rule.Name == "10% Quantity Discount (5+)");

        Assert.True(honeycrispVolumeDiscountRule.IsStackable);
        Assert.Equal(7, honeycrispVolumeDiscountRule.Priority);
        Assert.Single(honeycrispVolumeDiscountRule.Actions, action => action.ActionType == Models.ActionType.PercentageDiscount && action.Amount == 10.0000m && action.CalculationBase == Models.CalculationBase.RunningTotal);
        Assert.Contains(honeycrispVolumeDiscountRule.Conditions, condition => condition.Attribute == "Quantity" && condition.Operator == ">=" && condition.Value == "5");
        Assert.Single(honeycrispVolumeDiscountRule.TargetVariants, target => target.VariantId == appleVariant.Id);
        Assert.True(honeycrispSeasonSaleRule.Priority > honeycrispVolumeDiscountRule.Priority);

        var cherryRule = await dbContext.PriceRules
            .Include(rule => rule.Actions)
            .Include(rule => rule.Conditions)
            .Include(rule => rule.TargetVariants)
            .SingleAsync(rule => rule.Name == "10% Quantity Discount (2+)");

        Assert.True(cherryRule.IsStackable);
        Assert.Equal(15, cherryRule.Priority);
        Assert.Single(cherryRule.Actions, action => action.ActionType == Models.ActionType.PercentageDiscount && action.Amount == 10.0000m);
        Assert.Contains(cherryRule.Conditions, condition => condition.Attribute == "Quantity" && condition.Operator == ">=" && condition.Value == "2");
        Assert.Single(cherryRule.TargetVariants, target => target.VariantId == cherryVariant.Id);
    }

    [Fact]
    public async Task RenameSeededPriceRules_migration_updates_existing_rules_without_duplicates()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<FruitShopDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new FruitShopDbContext(options);
        var migrator = dbContext.GetService<IMigrator>();
        await migrator.MigrateAsync("20260919060445_AddOrderStatus");

        dbContext.PriceRules.AddRange(
            new PriceRule { Name = "Retail Citrus Bulk Discount" },
            new PriceRule { Name = "VIP Discount" },
            new PriceRule { Name = "Wholesale Avocado Override" },
            new PriceRule { Name = "Honeycrisp Apples Season Sale" },
            new PriceRule { Name = "Honeycrisp Apples Volume Discount" },
            new PriceRule { Name = "Cherry Volume Discount" });
        await dbContext.SaveChangesAsync();

        await migrator.MigrateAsync();
        await FruitShopDemoSeeder.SeedAsync(dbContext);
        dbContext.ChangeTracker.Clear();

        var ruleNames = await dbContext.PriceRules
            .AsNoTracking()
            .OrderBy(rule => rule.Name)
            .Select(rule => rule.Name)
            .ToListAsync();

        Assert.Equal(
        [
            "10% Quantity Discount (2+)",
            "10% Quantity Discount (5+)",
            "Autumn $2.99 Price Override",
            "Retail 10% Quantity Discount (3+)",
            "VIP 5% Discount",
            "Wholesale $1.25 Quantity Price Override (10+)"
        ],
        ruleNames);
    }

    [Fact]
    public async Task GetVariantByIdAsync_returns_variant_with_unit_of_measure()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<FruitShopDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new FruitShopDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
        await FruitShopDemoSeeder.SeedAsync(dbContext);

        var variantId = await dbContext.ProductVariants
            .Where(variant => variant.Sku == "APPLE-HONEY-1KG")
            .Select(variant => variant.Id)
            .SingleAsync();

        IProductRepository repository = new ProductRepository(dbContext);
        var variant = await repository.GetVariantByIdAsync(variantId);

        Assert.NotNull(variant);
        Assert.Equal("APPLE-HONEY-1KG", variant!.Sku);
        Assert.Equal("kg", variant.UnitOfMeasure.Code);
    }

    [Fact]
    public async Task GetActiveRulesAsync_includes_explicitly_global_rules()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<FruitShopDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new FruitShopDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
        await FruitShopDemoSeeder.SeedAsync(dbContext);

        var avocadoVariantId = await dbContext.ProductVariants
            .Where(variant => variant.Sku == "AVOCADO-HASS-EACH")
            .Select(variant => variant.Id)
            .SingleAsync();
        var globalRule = new PriceRule
        {
            Name = "Global Retail Discount",
            Priority = 20,
            IsStackable = true,
            AppliesToAllVariants = true,
            Conditions =
            [
                new PriceRuleCondition { Attribute = "CustomerTier", Operator = "=", Value = "RETAIL" }
            ],
            Actions =
            [
                new PriceRuleAction { ActionType = ActionType.PercentageDiscount, Amount = 5m }
            ]
        };
        dbContext.PriceRules.Add(globalRule);
        await dbContext.SaveChangesAsync();

        var rulesForAvocado = await new PriceRuleRepository(dbContext)
            .GetActiveRulesAsync(avocadoVariantId, "RETAIL");

        Assert.Contains(rulesForAvocado, rule => rule.Id == globalRule.Id);
    }

    [Fact]
    public async Task GetProductsAsync_returns_variants_with_unit_of_measure()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<FruitShopDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new FruitShopDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
        await FruitShopDemoSeeder.SeedAsync(dbContext);

        IProductRepository repository = new ProductRepository(dbContext);
        var variant = (await repository.GetProductsAsync(new PageRequest()))
            .Items
            .SelectMany(product => product.Variants)
            .Single(item => item.Sku == "APPLE-HONEY-1KG");

        Assert.Equal("kg", variant.UnitOfMeasure.Code);
        Assert.Equal(MeasureType.Weight, variant.UnitOfMeasure.MeasureType);
    }

    [Fact]
    public async Task GetProductsAsync_returns_a_bounded_page_with_a_total_count()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<FruitShopDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new FruitShopDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
        await FruitShopDemoSeeder.SeedAsync(dbContext);

        var page = await new ProductRepository(dbContext).GetProductsAsync(new PageRequest
        {
            Page = 2,
            PageSize = 3
        });

        Assert.Equal(2, page.Page);
        Assert.Equal(3, page.PageSize);
        Assert.Equal(7, page.TotalCount);
        Assert.Equal(3, page.Items.Count);
        Assert.Equal(["Hass Avocados", "Honeycrisp Apples", "Kiwi"], page.Items.Select(product => product.Name));
    }

    [Fact]
    public async Task GetActiveRulesAsync_filters_rules_by_customer_tier_and_variant()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<FruitShopDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new FruitShopDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
        await FruitShopDemoSeeder.SeedAsync(dbContext);

        var appleVariantId = await dbContext.ProductVariants
            .Where(variant => variant.Sku == "APPLE-HONEY-1KG")
            .Select(variant => variant.Id)
            .SingleAsync();

        var orangeVariantId = await dbContext.ProductVariants
            .Where(variant => variant.Sku == "ORANGE-NAVEL-1KG")
            .Select(variant => variant.Id)
            .SingleAsync();

        var repository = new PriceRuleRepository(dbContext);

        var appleRules = await repository.GetActiveRulesAsync(appleVariantId, "RETAIL");
        var retailOrangeRules = await repository.GetActiveRulesAsync(orangeVariantId, "RETAIL");
        var vipOrangeRules = await repository.GetActiveRulesAsync(orangeVariantId, "VIP");

        Assert.Equal([8, 7], appleRules.Select(rule => rule.Priority));
        Assert.All(appleRules, rule =>
        {
            Assert.NotEmpty(rule.Conditions);
            Assert.NotEmpty(rule.Actions);
        });

        var orangeRule = Assert.Single(retailOrangeRules);
        Assert.Equal("Retail 10% Quantity Discount (3+)", orangeRule.Name);

        var vipRule = Assert.Single(vipOrangeRules);
        Assert.Equal("VIP 5% Discount", vipRule.Name);
    }

    [Fact]
    public async Task GetByIdAsync_returns_order_with_items_and_calculated_totals()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<FruitShopDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new FruitShopDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
        await FruitShopDemoSeeder.SeedAsync(dbContext);

        var variant = await dbContext.ProductVariants
            .SingleAsync(variant => variant.Sku == "APPLE-HONEY-1KG");

        var order = new Order { CustomerTier = "RETAIL" };
        order.AddItem(variant, 2.5m, 3.21m, "Honeycrisp sale");

        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        var repository = new OrderRepository(dbContext);
        var savedOrder = await repository.GetByIdAsync(order.Id);

        Assert.NotNull(savedOrder);

        var item = Assert.Single(savedOrder!.Items);
        Assert.Equal(variant.Id, item.VariantId);
        Assert.Equal(2.5m, item.Quantity);
        Assert.Equal(3.21m, item.UnitPriceApplied);
        Assert.Equal(8.03m, item.TotalLineAmount);
        Assert.Equal("Honeycrisp sale", item.PriceChangeReason);
        Assert.Equal("APPLE-HONEY-1KG", item.Variant.Sku);
    }

    [Fact]
    public async Task Saving_orders_stamps_audit_fields_in_utc()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<FruitShopDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new FruitShopDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
        await FruitShopDemoSeeder.SeedAsync(dbContext);

        var variant = await dbContext.ProductVariants.SingleAsync(item => item.Sku == "APPLE-HONEY-1KG");
        var order = new Order { CustomerTier = "RETAIL", UpdateAt = DateTime.UnixEpoch };
        order.AddItem(variant, 1m, 3m);
        var item = Assert.Single(order.Items);
        item.CreateAt = DateTime.UnixEpoch;
        item.UpdateAt = DateTime.UnixEpoch;

        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();

        Assert.Equal(DateTimeKind.Utc, order.UpdateAt.Kind);
        Assert.NotEqual(DateTime.UnixEpoch, order.UpdateAt);
        Assert.Equal(DateTimeKind.Utc, item.CreateAt.Kind);
        Assert.Equal(DateTimeKind.Utc, item.UpdateAt.Kind);

        order.UpdateAt = DateTime.UnixEpoch;
        item.UpdateAt = DateTime.UnixEpoch;
        item.Quantity = 2m;
        await dbContext.SaveChangesAsync();

        Assert.Equal(DateTimeKind.Utc, order.UpdateAt.Kind);
        Assert.NotEqual(DateTime.UnixEpoch, order.UpdateAt);
        Assert.Equal(DateTimeKind.Utc, item.UpdateAt.Kind);
        Assert.NotEqual(DateTime.UnixEpoch, item.UpdateAt);
    }

    [Fact]
    public void Order_updates_an_existing_item_when_the_variant_is_added_again()
    {
        var order = new Order { CustomerTier = "RETAIL" };
        var variant = new ProductVariant
        {
            Id = 1,
            Sku = "PRODUCT-VARIANT-A",
            UomCode = "kg"
        };

        order.AddItem(variant, 2m, 4m, "Base price applied");
        order.AddItem(variant, 3m, 3.5m, "Volume discount applied");

        var item = Assert.Single(order.Items);
        Assert.Equal(3m, item.Quantity);
        Assert.Equal(3.5m, item.UnitPriceApplied);
        Assert.Equal(10.5m, item.TotalLineAmount);
        Assert.Equal("Volume discount applied", item.PriceChangeReason);
        Assert.Equal(10.5m, order.TotalAmount);
    }

    [Fact]
    public void Submitted_orders_cannot_be_changed()
    {
        var order = new Order { CustomerTier = "RETAIL" };
        var variant = new ProductVariant
        {
            Id = 1,
            Sku = "PRODUCT-VARIANT-A",
            UomCode = "kg"
        };

        order.AddItem(variant, 1m, 4m);
        order.Submit();

        Assert.Equal(OrderStatus.Submitted, order.Status);
        Assert.Throws<InvalidOperationException>(() => order.AddItem(variant, 2m, 4m));
        Assert.Throws<InvalidOperationException>(order.Submit);
    }

    [Fact]
    public async Task OrderProcessingService_creates_and_submits_all_items_atomically()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<FruitShopDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new FruitShopDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
        await FruitShopDemoSeeder.SeedAsync(dbContext);

        var variantIds = await dbContext.ProductVariants
            .OrderBy(variant => variant.Id)
            .Select(variant => variant.Id)
            .Take(2)
            .ToListAsync();
        var pricingFactory = new PricingStrategyFactory(
            [new QuantityConditionEvaluator(), new DateConditionEvaluator(), new CustomerTierConditionEvaluator(), new CartSubtotalConditionEvaluator()],
            [new PercentageDiscountStrategy(), new AmountOffStrategy(), new OverridePriceStrategy()]);
        var service = new OrderProcessingService(
            dbContext,
            new OrderRepository(dbContext),
            new ProductRepository(dbContext),
            new OrderItemCalculationService(
                new ProductRepository(dbContext),
                new PriceRuleRepository(dbContext),
                new PricingService(pricingFactory)));

        var order = await service.CreateAndSubmitOrderAsync(
            "RETAIL",
            [new OrderItemSubmission(variantIds[0], 1m), new OrderItemSubmission(variantIds[1], 2m)]);

        Assert.Equal(OrderStatus.Submitted, order.Status);
        Assert.Equal(2, order.Items.Count);
        Assert.True(order.TotalAmount > 0m);

        dbContext.ChangeTracker.Clear();
        var persistedOrder = await new OrderRepository(dbContext).GetByIdAsync(order.Id);

        Assert.NotNull(persistedOrder);
        Assert.Equal(OrderStatus.Submitted, persistedOrder!.Status);
        Assert.Equal(2, persistedOrder.Items.Count);
    }

    [Fact]
    public async Task OrderProcessingService_rolls_back_when_a_later_item_is_invalid()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<FruitShopDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new FruitShopDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
        await FruitShopDemoSeeder.SeedAsync(dbContext);

        var validVariantId = await dbContext.ProductVariants
            .OrderBy(variant => variant.Id)
            .Select(variant => variant.Id)
            .FirstAsync();
        var pricingFactory = new PricingStrategyFactory(
            [new QuantityConditionEvaluator(), new DateConditionEvaluator(), new CustomerTierConditionEvaluator(), new CartSubtotalConditionEvaluator()],
            [new PercentageDiscountStrategy(), new AmountOffStrategy(), new OverridePriceStrategy()]);
        var service = new OrderProcessingService(
            dbContext,
            new OrderRepository(dbContext),
            new ProductRepository(dbContext),
            new OrderItemCalculationService(
                new ProductRepository(dbContext),
                new PriceRuleRepository(dbContext),
                new PricingService(pricingFactory)));

        await Assert.ThrowsAsync<NotFoundException>(() => service.CreateAndSubmitOrderAsync(
            "RETAIL",
            [new OrderItemSubmission(validVariantId, 1m), new OrderItemSubmission(long.MaxValue, 1m)]));

        dbContext.ChangeTracker.Clear();
        Assert.Empty(await dbContext.Orders.AsNoTracking().ToListAsync());
        Assert.Empty(await dbContext.OrderItems.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task OrderItemCalculationService_calculates_a_preview_without_creating_order_items()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<FruitShopDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new FruitShopDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
        await FruitShopDemoSeeder.SeedAsync(dbContext);

        var variant = await dbContext.ProductVariants
            .SingleAsync(item => item.Sku == "ORANGE-NAVEL-1KG");
        var pricingFactory = new PricingStrategyFactory(
            [new QuantityConditionEvaluator(), new DateConditionEvaluator(), new CustomerTierConditionEvaluator(), new CartSubtotalConditionEvaluator()],
            [new PercentageDiscountStrategy(), new AmountOffStrategy(), new OverridePriceStrategy()]);
        IOrderItemCalculationService service = new OrderItemCalculationService(
            new ProductRepository(dbContext),
            new PriceRuleRepository(dbContext),
            new PricingService(pricingFactory));

        var preview = await service.CalculateAsync(variant.Id, "RETAIL", 3m, 0m);

        Assert.NotNull(preview);
        Assert.Equal(3m, preview!.Quantity);
        Assert.Equal(Math.Round(variant.BasePrice * 0.9m, 2, MidpointRounding.AwayFromZero), preview.PriceCalculation.UnitPriceApplied);
        Assert.Equal("Retail 10% Quantity Discount (3+)", Assert.Single(preview.ActiveRules).Name);
        Assert.Empty(dbContext.Orders);
        Assert.Empty(dbContext.OrderItems);
    }

    [Fact]
    public async Task OrderItemCalculationService_uses_the_requested_pricing_date_for_a_quote()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<FruitShopDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new FruitShopDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
        await FruitShopDemoSeeder.SeedAsync(dbContext);

        var variant = await dbContext.ProductVariants
            .SingleAsync(item => item.Sku == "APPLE-HONEY-1KG");
        var pricingFactory = new PricingStrategyFactory(
            [new QuantityConditionEvaluator(), new DateConditionEvaluator(), new CustomerTierConditionEvaluator(), new CartSubtotalConditionEvaluator()],
            [new PercentageDiscountStrategy(), new AmountOffStrategy(), new OverridePriceStrategy()]);
        IOrderItemCalculationService service = new OrderItemCalculationService(
            new ProductRepository(dbContext),
            new PriceRuleRepository(dbContext),
            new PricingService(pricingFactory));

        var seasonalQuote = await service.QuoteAsync(
            new OrderItemQuoteRequest(variant, "RETAIL", 1m, 0m, new DateTime(2026, 9, 15)));
        var outOfSeasonQuote = await service.QuoteAsync(
            new OrderItemQuoteRequest(variant, "RETAIL", 1m, 0m, new DateTime(2026, 12, 1)));

        Assert.Equal(2.9900m, seasonalQuote.PriceCalculation.UnitPriceApplied);
        Assert.Equal(variant.BasePrice, outOfSeasonQuote.PriceCalculation.UnitPriceApplied);
    }

    [Fact]
    public void Model_navigation_graphs_serialize_without_cycles()
    {
        var product = new Product { Name = "Honeycrisp Apples" };
        var variant = new ProductVariant
        {
            Product = product,
            Sku = "APPLE-HONEY-1KG",
            UomCode = "kg",
            UnitOfMeasure = new UnitOfMeasure { Code = "kg", MeasureType = MeasureType.Weight }
        };
        product.Variants.Add(variant);

        var rule = new PriceRule { Name = "Honeycrisp Sale" };
        rule.Conditions.Add(new PriceRuleCondition { Rule = rule, Attribute = "Quantity", Operator = "GreaterThan", Value = "2" });
        rule.Actions.Add(new PriceRuleAction { Rule = rule, ActionType = ActionType.PercentageDiscount, Amount = 10m });

        var variantTarget = new RuleTargetVariant { Rule = rule, Variant = variant };
        rule.TargetVariants.Add(variantTarget);
        variant.TargetRules.Add(variantTarget);
        rule.TargetCustomerTiers.Add(new RuleTargetCustomerTier { Rule = rule, CustomerTier = "RETAIL" });

        var order = new Order { CustomerTier = "RETAIL" };
        order.Items.Add(new OrderItem { Order = order, Variant = variant, Quantity = 1m });

        var productJson = JsonSerializer.Serialize(product);
        var orderJson = JsonSerializer.Serialize(order);
        var priceRuleJson = JsonSerializer.Serialize(rule);

        Assert.DoesNotContain("\"Product\"", productJson);
        Assert.DoesNotContain("\"Order\"", orderJson);
        Assert.DoesNotContain("\"Rule\"", priceRuleJson);
        Assert.DoesNotContain("\"Variant\"", priceRuleJson);
    }
}
