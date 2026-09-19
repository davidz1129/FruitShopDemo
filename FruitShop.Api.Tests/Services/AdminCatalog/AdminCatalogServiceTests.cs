using AutoMapper;
using FluentAssertions;
using FruitShop.Api.Data;
using FruitShop.Api.Models;
using FruitShop.Api.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FruitShop.Api.Tests;

public sealed class AdminCatalogServiceTests
{
    [Fact]
    public async Task GetCatalogAsync_ShouldReturnMappedPages_WhenCatalogContainsData()
    {
        // Arrange
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var dbContext = database.CreateContext();
        dbContext.UnitsOfMeasure.Add(new UnitOfMeasure { Code = "kg", MeasureType = MeasureType.Weight });
        dbContext.Products.AddRange(new Product { Name = "Bananas" }, new Product { Name = "Apples" });
        dbContext.PriceRules.Add(new PriceRule { Name = "Autumn", Priority = 3, AppliesToAllVariants = true });
        await dbContext.SaveChangesAsync();
        var mapper = new Mock<IMapper>();
        mapper.Setup(candidate => candidate.Map<List<AdminProductResponse>>(It.IsAny<object>()))
            .Returns([new AdminProductResponse(2, "Apples", [])]);
        mapper.Setup(candidate => candidate.Map<List<UnitOfMeasureResponse>>(It.IsAny<object>()))
            .Returns([new UnitOfMeasureResponse("kg", MeasureType.Weight)]);
        mapper.Setup(candidate => candidate.Map<List<AdminPriceRuleResponse>>(It.IsAny<object>()))
            .Returns([new AdminPriceRuleResponse(1, "Autumn", 3, false, true, RuleStatus.Active, [], [], [], [])]);
        var service = new AdminCatalogService(dbContext, mapper.Object);

        // Act
        var result = await service.GetCatalogAsync(new AdminCatalogPageRequest { ProductPageSize = 1, PriceRulePageSize = 1 });

        // Assert
        result.Products.TotalCount.Should().Be(2);
        result.Products.Items.Should().ContainSingle().Which.Name.Should().Be("Apples");
        result.UnitsOfMeasure.Should().ContainSingle().Which.Code.Should().Be("kg");
        result.PriceRules.TotalCount.Should().Be(1);
        mapper.Verify(candidate => candidate.Map<List<AdminProductResponse>>(It.IsAny<object>()), Times.Once);
        mapper.Verify(candidate => candidate.Map<List<UnitOfMeasureResponse>>(It.IsAny<object>()), Times.Once);
        mapper.Verify(candidate => candidate.Map<List<AdminPriceRuleResponse>>(It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task CreateProductAsync_ShouldPersistAndMapProduct_WhenRequestIsValid()
    {
        // Arrange
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var dbContext = database.CreateContext();
        var mapper = new Mock<IMapper>();
        var request = new CreateAdminProductRequest { Name = "Pears" };
        var product = new Product { Name = "Pears" };
        var response = new AdminProductResponse(1, "Pears", []);
        mapper.Setup(candidate => candidate.Map<Product>(request)).Returns(product);
        mapper.Setup(candidate => candidate.Map<AdminProductResponse>(product)).Returns(response);
        var service = new AdminCatalogService(dbContext, mapper.Object);

        // Act
        var result = await service.CreateProductAsync(request);

        // Assert
        result.Should().BeSameAs(response);
        (await dbContext.Products.SingleAsync()).Name.Should().Be("Pears");
        mapper.Verify(candidate => candidate.Map<Product>(request), Times.Once);
        mapper.Verify(candidate => candidate.Map<AdminProductResponse>(product), Times.Once);
    }

    [Fact]
    public async Task CreateVariantAsync_ShouldReturnProductNotFound_WhenProductIsMissing()
    {
        // Arrange
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var dbContext = database.CreateContext();
        var mapper = new Mock<IMapper>();
        var service = new AdminCatalogService(dbContext, mapper.Object);

        // Act
        var result = await service.CreateVariantAsync(99, CreateVariantRequest());

        // Assert
        result.Status.Should().Be(AdminCatalogOperationStatus.ProductNotFound);
        mapper.Verify(candidate => candidate.Map<ProductVariant>(It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task CreateVariantAsync_ShouldReturnUnitOfMeasureNotFound_WhenUomDoesNotExist()
    {
        // Arrange
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var dbContext = database.CreateContext();
        var product = new Product { Name = "Apples" };
        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync();
        var service = new AdminCatalogService(dbContext, Mock.Of<IMapper>());

        // Act
        var result = await service.CreateVariantAsync(product.Id, CreateVariantRequest());

        // Assert
        result.Status.Should().Be(AdminCatalogOperationStatus.UnitOfMeasureNotFound);
        result.Message.Should().Be("Choose a valid unit of measure.");
    }

    [Fact]
    public async Task CreateVariantAsync_ShouldReturnDuplicateSku_WhenSkuAlreadyExists()
    {
        // Arrange
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var dbContext = database.CreateContext();
        var product = await AddProductWithUnitAsync(dbContext, "Apples");
        dbContext.ProductVariants.Add(new ProductVariant { ProductId = product.Id, Sku = "APPLE", UomCode = "kg", UomFactor = 1m, BasePrice = 2m });
        await dbContext.SaveChangesAsync();
        var service = new AdminCatalogService(dbContext, Mock.Of<IMapper>());

        // Act
        var result = await service.CreateVariantAsync(product.Id, CreateVariantRequest());

        // Assert
        result.Status.Should().Be(AdminCatalogOperationStatus.DuplicateSku);
        result.Message.Should().Be("SKU values must be unique.");
    }

    [Fact]
    public async Task CreateVariantAsync_ShouldPersistMappedVariant_WhenRequestIsValid()
    {
        // Arrange
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var dbContext = database.CreateContext();
        var product = await AddProductWithUnitAsync(dbContext, "Pears");
        var mapper = new Mock<IMapper>();
        var request = CreateVariantRequest("PEAR");
        var variant = new ProductVariant { Sku = "PEAR", UomCode = "kg", UomFactor = 1m, BasePrice = 4m };
        var response = new AdminProductVariantResponse(1, "PEAR", "kg", 1m, 4m, true, MeasureType.Weight);
        mapper.Setup(candidate => candidate.Map<ProductVariant>(request)).Returns(variant);
        mapper.Setup(candidate => candidate.Map<AdminProductVariantResponse>(variant)).Returns(response);
        var service = new AdminCatalogService(dbContext, mapper.Object);

        // Act
        var result = await service.CreateVariantAsync(product.Id, request);

        // Assert
        result.Status.Should().Be(AdminCatalogOperationStatus.Success);
        result.Value.Should().BeSameAs(response);
        (await dbContext.ProductVariants.SingleAsync()).ProductId.Should().Be(product.Id);
        mapper.Verify(candidate => candidate.Map<AdminProductVariantResponse>(variant), Times.Once);
    }

    [Theory]
    [InlineData(42, AdminCatalogOperationStatus.VariantNotFound)]
    public async Task SetVariantStatusAsync_ShouldReturnVariantNotFound_WhenVariantIsMissing(long variantId, AdminCatalogOperationStatus expectedStatus)
    {
        // Arrange
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var dbContext = database.CreateContext();
        var service = new AdminCatalogService(dbContext, Mock.Of<IMapper>());

        // Act
        var result = await service.SetVariantStatusAsync(variantId, new SetProductVariantStatusRequest { IsActive = false });

        // Assert
        result.Status.Should().Be(expectedStatus);
    }

    [Fact]
    public async Task SetVariantStatusAsync_ShouldPersistStatusAndMapVariant_WhenVariantExists()
    {
        // Arrange
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var dbContext = database.CreateContext();
        var product = await AddProductWithUnitAsync(dbContext, "Apples");
        var variant = new ProductVariant { ProductId = product.Id, Sku = "APPLE", UomCode = "kg", UomFactor = 1m, BasePrice = 2m };
        dbContext.ProductVariants.Add(variant);
        await dbContext.SaveChangesAsync();
        var mapper = new Mock<IMapper>();
        var response = new AdminProductVariantResponse(variant.Id, "APPLE", "kg", 1m, 2m, false, MeasureType.Weight);
        mapper.Setup(candidate => candidate.Map<AdminProductVariantResponse>(It.IsAny<ProductVariant>())).Returns(response);
        var service = new AdminCatalogService(dbContext, mapper.Object);

        // Act
        var result = await service.SetVariantStatusAsync(variant.Id, new SetProductVariantStatusRequest { IsActive = false });

        // Assert
        result.Status.Should().Be(AdminCatalogOperationStatus.Success);
        result.Value.Should().BeSameAs(response);
        (await dbContext.ProductVariants.SingleAsync()).IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task CreatePriceRuleAsync_ShouldReturnInvalidRule_WhenScopeHasNoVariants()
    {
        // Arrange
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var dbContext = database.CreateContext();
        var service = new AdminCatalogService(dbContext, Mock.Of<IMapper>());

        // Act
        var result = await service.CreatePriceRuleAsync(CreatePriceRuleRequest(false));

        // Assert
        result.Status.Should().Be(AdminCatalogOperationStatus.InvalidRule);
        result.Message.Should().Contain("Select at least one");
    }

    [Fact]
    public async Task CreatePriceRuleAsync_ShouldReturnInvalidRule_WhenTargetVariantIsInactive()
    {
        // Arrange
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var dbContext = database.CreateContext();
        var product = await AddProductWithUnitAsync(dbContext, "Apples");
        var inactiveVariant = new ProductVariant { ProductId = product.Id, Sku = "APPLE", UomCode = "kg", UomFactor = 1m, BasePrice = 2m, IsActive = false };
        dbContext.ProductVariants.Add(inactiveVariant);
        await dbContext.SaveChangesAsync();
        var service = new AdminCatalogService(dbContext, Mock.Of<IMapper>());

        // Act
        var result = await service.CreatePriceRuleAsync(CreatePriceRuleRequest(false, [inactiveVariant.Id]));

        // Assert
        result.Status.Should().Be(AdminCatalogOperationStatus.InvalidRule);
        result.Message.Should().Contain("active product variants");
    }

    [Fact]
    public async Task CreatePriceRuleAsync_ShouldPersistMappedRule_WhenRequestIsValid()
    {
        // Arrange
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var dbContext = database.CreateContext();
        var mapper = new Mock<IMapper>();
        var request = CreatePriceRuleRequest(true);
        var rule = new PriceRule { Name = "VIP discount", AppliesToAllVariants = true };
        var response = new AdminPriceRuleResponse(1, "VIP discount", 1, true, true, RuleStatus.Active, [], [], [], []);
        mapper.Setup(candidate => candidate.Map<PriceRule>(request)).Returns(rule);
        mapper.Setup(candidate => candidate.Map<AdminPriceRuleResponse>(rule)).Returns(response);
        var service = new AdminCatalogService(dbContext, mapper.Object);

        // Act
        var result = await service.CreatePriceRuleAsync(request);

        // Assert
        result.Status.Should().Be(AdminCatalogOperationStatus.Success);
        result.Value.Should().BeSameAs(response);
        (await dbContext.PriceRules.SingleAsync()).Name.Should().Be("VIP discount");
        mapper.Verify(candidate => candidate.Map<AdminPriceRuleResponse>(rule), Times.Once);
    }

    [Fact]
    public async Task GetProductVariantPriceRulesAsync_ShouldSeparateAssignedAndAvailableScopedRules()
    {
        // Arrange
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var dbContext = database.CreateContext();
        var product = await AddProductWithUnitAsync(dbContext, "Apples");
        var variant = new ProductVariant { ProductId = product.Id, Sku = "APPLE", UomCode = "kg", UomFactor = 1m, BasePrice = 2m };
        dbContext.ProductVariants.Add(variant);
        await dbContext.SaveChangesAsync();
        var assignedRule = new PriceRule { Name = "Apple discount", Priority = 20, AppliesToAllVariants = false };
        var availableRule = new PriceRule { Name = "Harvest discount", Priority = 10, AppliesToAllVariants = false };
        var globalRule = new PriceRule { Name = "Store-wide discount", Priority = 30, AppliesToAllVariants = true };
        dbContext.PriceRules.AddRange(assignedRule, availableRule, globalRule);
        await dbContext.SaveChangesAsync();
        dbContext.RuleTargetVariants.Add(new RuleTargetVariant { RuleId = assignedRule.Id, VariantId = variant.Id });
        await dbContext.SaveChangesAsync();
        var mapper = new Mock<IMapper>();
        mapper.Setup(candidate => candidate.Map<AdminProductVariantResponse>(It.IsAny<ProductVariant>()))
            .Returns(new AdminProductVariantResponse(variant.Id, variant.Sku, "kg", 1m, 2m, true, MeasureType.Weight));
        mapper.Setup(candidate => candidate.Map<List<AdminPriceRuleResponse>>(It.IsAny<object>()))
            .Returns((object source) => ((IEnumerable<PriceRule>)source)
                .Select(rule => new AdminPriceRuleResponse(rule.Id, rule.Name, rule.Priority, rule.IsStackable, rule.AppliesToAllVariants, rule.Status, [], [], [], []))
                .ToList());
        var service = new AdminCatalogService(dbContext, mapper.Object);

        // Act
        var result = await service.GetProductVariantPriceRulesAsync(variant.Id);

        // Assert
        result.Status.Should().Be(AdminCatalogOperationStatus.Success);
        result.Value!.AssignedPriceRules.Select(rule => rule.Name).Should().Equal("Apple discount");
        result.Value.AvailablePriceRules.Select(rule => rule.Name).Should().Equal("Harvest discount");
        result.Value.GlobalPriceRules.Select(rule => rule.Name).Should().Equal("Store-wide discount");
    }

    [Fact]
    public async Task AssignPriceRuleToProductVariantAsync_ShouldPersistTarget_WhenVariantAndRuleAreEligible()
    {
        // Arrange
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var dbContext = database.CreateContext();
        var product = await AddProductWithUnitAsync(dbContext, "Apples");
        var variant = new ProductVariant { ProductId = product.Id, Sku = "APPLE", UomCode = "kg", UomFactor = 1m, BasePrice = 2m };
        var priceRule = new PriceRule { Name = "Apple discount", AppliesToAllVariants = false };
        dbContext.AddRange(variant, priceRule);
        await dbContext.SaveChangesAsync();
        var mapper = new Mock<IMapper>();
        mapper.Setup(candidate => candidate.Map<AdminPriceRuleResponse>(It.IsAny<PriceRule>()))
            .Returns(new AdminPriceRuleResponse(priceRule.Id, priceRule.Name, 0, false, false, RuleStatus.Active, [variant.Id], [], [], []));
        var service = new AdminCatalogService(dbContext, mapper.Object);

        // Act
        var result = await service.AssignPriceRuleToProductVariantAsync(variant.Id, priceRule.Id);

        // Assert
        result.Status.Should().Be(AdminCatalogOperationStatus.Success);
        (await dbContext.RuleTargetVariants.SingleAsync()).Should().BeEquivalentTo(new { RuleId = priceRule.Id, VariantId = variant.Id });
    }

    [Fact]
    public async Task RemovePriceRuleFromProductVariantAsync_ShouldDeleteTarget_WhenRuleIsAssigned()
    {
        // Arrange
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var dbContext = database.CreateContext();
        var product = await AddProductWithUnitAsync(dbContext, "Apples");
        var variant = new ProductVariant { ProductId = product.Id, Sku = "APPLE", UomCode = "kg", UomFactor = 1m, BasePrice = 2m };
        var priceRule = new PriceRule { Name = "Apple discount", AppliesToAllVariants = false };
        dbContext.AddRange(variant, priceRule);
        await dbContext.SaveChangesAsync();
        dbContext.RuleTargetVariants.Add(new RuleTargetVariant { RuleId = priceRule.Id, VariantId = variant.Id });
        await dbContext.SaveChangesAsync();
        var service = new AdminCatalogService(dbContext, Mock.Of<IMapper>());

        // Act
        var result = await service.RemovePriceRuleFromProductVariantAsync(variant.Id, priceRule.Id);

        // Assert
        result.Status.Should().Be(AdminCatalogOperationStatus.Success);
        (await dbContext.RuleTargetVariants.CountAsync()).Should().Be(0);
    }

    private static async Task<Product> AddProductWithUnitAsync(FruitShopDbContext dbContext, string name)
    {
        dbContext.UnitsOfMeasure.Add(new UnitOfMeasure { Code = "kg", MeasureType = MeasureType.Weight });
        var product = new Product { Name = name };
        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync();
        return product;
    }

    private static CreateProductVariantRequest CreateVariantRequest(string sku = "APPLE") =>
        new() { Sku = sku, UomCode = " kg ", UomFactor = 1m, BasePrice = 4m };

    private static CreatePriceRuleRequest CreatePriceRuleRequest(bool appliesToAllVariants, IReadOnlyList<long>? variantIds = null) =>
        new()
        {
            Name = "VIP discount",
            Priority = 1,
            IsStackable = true,
            AppliesToAllVariants = appliesToAllVariants,
            VariantIds = variantIds ?? [],
            Actions = [new CreatePriceRuleActionRequest { ActionType = ActionType.AmountOff, Amount = 1m }]
        };
}