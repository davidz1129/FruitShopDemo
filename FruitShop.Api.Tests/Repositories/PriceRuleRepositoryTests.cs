using FluentAssertions;
using FruitShop.Api.Models;
using FruitShop.Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FruitShop.Api.Tests;

public sealed class PriceRuleRepositoryTests
{
    [Fact]
    public async Task GetActiveRulesAsync_ShouldFilterTargetsAndSortByPriority_WhenRulesHaveMixedScopes()
    {
        // Arrange
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var dbContext = database.CreateContext();
        var (targetVariantId, otherVariantId) = await AddVariantsAsync(dbContext);
        dbContext.PriceRules.AddRange(
            new PriceRule { Name = "Global", Priority = 10, AppliesToAllVariants = true },
            new PriceRule
            {
                Name = "VIP specific",
                Priority = 20,
                TargetVariants = [new RuleTargetVariant { VariantId = targetVariantId }],
                TargetCustomerTiers = [new RuleTargetCustomerTier { CustomerTier = "VIP" }]
            },
            new PriceRule { Name = "Inactive", Priority = 100, AppliesToAllVariants = true, Status = RuleStatus.Inactive },
            new PriceRule { Name = "Other variant", Priority = 30, TargetVariants = [new RuleTargetVariant { VariantId = otherVariantId }] },
            new PriceRule
            {
                Name = "Wholesale only",
                Priority = 25,
                TargetVariants = [new RuleTargetVariant { VariantId = targetVariantId }],
                TargetCustomerTiers = [new RuleTargetCustomerTier { CustomerTier = "WHOLESALE" }]
            });
        await dbContext.SaveChangesAsync();
        var repository = new PriceRuleRepository(dbContext);

        // Act
        var result = await repository.GetActiveRulesAsync(targetVariantId, " vip ");

        // Assert
        result.Select(rule => rule.Name).Should().Equal("VIP specific", "Global");
        result.Should().OnlyContain(rule => rule.Status == RuleStatus.Active);
    }

    private static async Task<(long TargetVariantId, long OtherVariantId)> AddVariantsAsync(FruitShop.Api.Data.FruitShopDbContext dbContext)
    {
        dbContext.UnitsOfMeasure.Add(new UnitOfMeasure { Code = "kg", MeasureType = MeasureType.Weight });
        var product = new Product { Name = "Apples" };
        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync();
        dbContext.ProductVariants.AddRange(
            new ProductVariant { ProductId = product.Id, Sku = "APPLE-1", UomCode = "kg", UomFactor = 1m, BasePrice = 2m },
            new ProductVariant { ProductId = product.Id, Sku = "APPLE-2", UomCode = "kg", UomFactor = 1m, BasePrice = 3m });
        await dbContext.SaveChangesAsync();
        var ids = await dbContext.ProductVariants.OrderBy(variant => variant.Id).Select(variant => variant.Id).ToListAsync();
        return (ids[0], ids[1]);
    }
}