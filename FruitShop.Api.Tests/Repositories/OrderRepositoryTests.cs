using FluentAssertions;
using FruitShop.Api.Models;
using FruitShop.Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FruitShop.Api.Tests;

public sealed class OrderRepositoryTests
{
    [Fact]
    public async Task AddAsync_ShouldPersistOrder_WhenOrderIsNew()
    {
        // Arrange
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var dbContext = database.CreateContext();
        var repository = new OrderRepository(dbContext);
        var order = new Order { CustomerTier = "VIP" };

        // Act
        var result = await repository.AddAsync(order);

        // Assert
        result.Should().BeSameAs(order);
        result.Id.Should().BePositive();
        (await dbContext.Orders.SingleAsync()).CustomerTier.Should().Be("VIP");
    }

    [Fact]
    public async Task GetAllAsync_ShouldOrderNewestFirstAndPageResults_WhenOrdersExist()
    {
        // Arrange
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var dbContext = database.CreateContext();
        dbContext.Orders.AddRange(
            new Order { CustomerTier = "RETAIL", CreatedAt = new DateTime(2026, 9, 17) },
            new Order { CustomerTier = "VIP", CreatedAt = new DateTime(2026, 9, 19) },
            new Order { CustomerTier = "WHOLESALE", CreatedAt = new DateTime(2026, 9, 18) });
        await dbContext.SaveChangesAsync();
        var repository = new OrderRepository(dbContext);

        // Act
        var result = await repository.GetAllAsync(new PageRequest { Page = 1, PageSize = 2 });

        // Assert
        result.TotalCount.Should().Be(3);
        result.Items.Select(order => order.CustomerTier).Should().Equal("VIP", "WHOLESALE");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldIncludeItemVariantAndUnit_WhenOrderExists()
    {
        // Arrange
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var dbContext = database.CreateContext();
        var variant = await AddVariantAsync(dbContext);
        var order = new Order { CustomerTier = "VIP" };
        order.AddItem(variant, 2m, 3m, "Promotion");
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();
        var repository = new OrderRepository(dbContext);

        // Act
        var result = await repository.GetByIdAsync(order.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Items.Should().ContainSingle();
        result.Items.Single().Variant.Sku.Should().Be("APPLE");
        result.Items.Single().Variant.UnitOfMeasure.Code.Should().Be("kg");
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldPersistTrackedChanges_WhenEntityIsModified()
    {
        // Arrange
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var dbContext = database.CreateContext();
        var order = new Order { CustomerTier = "VIP" };
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();
        var repository = new OrderRepository(dbContext);
        order.CustomerTier = "RETAIL";

        // Act
        await repository.SaveChangesAsync();

        // Assert
        (await dbContext.Orders.SingleAsync()).CustomerTier.Should().Be("RETAIL");
    }

    private static async Task<ProductVariant> AddVariantAsync(FruitShop.Api.Data.FruitShopDbContext dbContext)
    {
        dbContext.UnitsOfMeasure.Add(new UnitOfMeasure { Code = "kg", MeasureType = MeasureType.Weight });
        var product = new Product { Name = "Apples" };
        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync();
        var variant = new ProductVariant { ProductId = product.Id, Sku = "APPLE", UomCode = "kg", UomFactor = 1m, BasePrice = 3m };
        dbContext.ProductVariants.Add(variant);
        await dbContext.SaveChangesAsync();
        return variant;
    }
}