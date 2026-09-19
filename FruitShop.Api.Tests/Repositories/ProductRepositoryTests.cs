using FluentAssertions;
using FruitShop.Api.Models;
using FruitShop.Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FruitShop.Api.Tests;

public sealed class ProductRepositoryTests
{
    [Fact]
    public async Task GetProductsAsync_ShouldReturnOnlyProductsWithActiveVariants_WhenCatalogContainsMixedAvailability()
    {
        // Arrange
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var dbContext = database.CreateContext();
        var kilogram = new UnitOfMeasure { Code = "kg", MeasureType = MeasureType.Weight };
        dbContext.UnitsOfMeasure.Add(kilogram);
        dbContext.Products.AddRange(
            new Product
            {
                Name = "Bananas",
                Variants = [new ProductVariant { Sku = "BANANA-1", UomCode = "kg", UomFactor = 1m, BasePrice = 3m, IsActive = false }]
            },
            new Product
            {
                Name = "Apples",
                Variants =
                [
                    new ProductVariant { Sku = "APPLE-Z", UomCode = "kg", UomFactor = 1m, BasePrice = 4m, IsActive = true },
                    new ProductVariant { Sku = "APPLE-A", UomCode = "kg", UomFactor = 1m, BasePrice = 2m, IsActive = true },
                    new ProductVariant { Sku = "APPLE-OLD", UomCode = "kg", UomFactor = 1m, BasePrice = 1m, IsActive = false }
                ]
            });
        await dbContext.SaveChangesAsync();
        var repository = new ProductRepository(dbContext);

        // Act
        var result = await repository.GetProductsAsync(new PageRequest { Page = 1, PageSize = 10 });

        // Assert
        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle().Which.Name.Should().Be("Apples");
        result.Items.Single().Variants.Select(variant => variant.Sku).Should().Equal("APPLE-A", "APPLE-Z");
        result.Items.Single().Variants.Should().OnlyContain(variant => variant.IsActive);
    }

    [Fact]
    public async Task GetVariantByIdAsync_ShouldReturnActiveVariantWithUnit_WhenVariantIsActive()
    {
        // Arrange
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var dbContext = database.CreateContext();
        var activeVariantId = await AddVariantsAsync(dbContext);
        var repository = new ProductRepository(dbContext);

        // Act
        var result = await repository.GetVariantByIdAsync(activeVariantId);

        // Assert
        result.Should().NotBeNull();
        result!.UnitOfMeasure.Code.Should().Be("kg");
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetVariantByIdAsync_ShouldReturnNull_WhenVariantIsInactiveOrMissing()
    {
        // Arrange
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var dbContext = database.CreateContext();
        await AddVariantsAsync(dbContext);
        var inactiveId = await dbContext.ProductVariants.Where(variant => !variant.IsActive).Select(variant => variant.Id).SingleAsync();
        var repository = new ProductRepository(dbContext);

        // Act
        var inactiveResult = await repository.GetVariantByIdAsync(inactiveId);
        var missingResult = await repository.GetVariantByIdAsync(999);

        // Assert
        inactiveResult.Should().BeNull();
        missingResult.Should().BeNull();
    }

    private static async Task<long> AddVariantsAsync(FruitShop.Api.Data.FruitShopDbContext dbContext)
    {
        dbContext.UnitsOfMeasure.Add(new UnitOfMeasure { Code = "kg", MeasureType = MeasureType.Weight });
        dbContext.Products.Add(new Product
        {
            Name = "Apples",
            Variants =
            [
                new ProductVariant { Sku = "APPLE-ACTIVE", UomCode = "kg", UomFactor = 1m, BasePrice = 2m, IsActive = true },
                new ProductVariant { Sku = "APPLE-INACTIVE", UomCode = "kg", UomFactor = 1m, BasePrice = 2m, IsActive = false }
            ]
        });
        await dbContext.SaveChangesAsync();
        return await dbContext.ProductVariants.Where(variant => variant.IsActive).Select(variant => variant.Id).SingleAsync();
    }
}