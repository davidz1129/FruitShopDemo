using FluentAssertions;
using FruitShop.Api.Features.AdminCatalog;
using FruitShop.Api.Models;
using FruitShop.Api.Services;
using Moq;

namespace FruitShop.Api.Tests;

public sealed class ManageProductVariantPriceRulesCommandHandlerTests
{
    [Fact]
    public async Task GetHandler_ShouldReturnServiceResult()
    {
        // Arrange
        var catalogService = new Mock<IAdminCatalogService>();
        var result = new AdminCatalogOperationResult<ProductVariantPriceRulesResponse>(AdminCatalogOperationStatus.VariantNotFound, null);
        catalogService.Setup(candidate => candidate.GetProductVariantPriceRulesAsync(7, CancellationToken.None)).ReturnsAsync(result);
        var handler = new GetProductVariantPriceRulesQueryHandler(catalogService.Object);

        // Act
        var actual = await handler.Handle(new GetProductVariantPriceRulesQuery(7), CancellationToken.None);

        // Assert
        actual.Should().BeSameAs(result);
    }

    [Fact]
    public async Task AssignHandler_ShouldReturnServiceResult()
    {
        // Arrange
        var catalogService = new Mock<IAdminCatalogService>();
        var result = new AdminCatalogOperationResult<AdminPriceRuleResponse>(AdminCatalogOperationStatus.PriceRuleNotFound, null);
        catalogService.Setup(candidate => candidate.AssignPriceRuleToProductVariantAsync(7, 8, CancellationToken.None)).ReturnsAsync(result);
        var handler = new AssignPriceRuleToProductVariantCommandHandler(catalogService.Object);

        // Act
        var actual = await handler.Handle(new AssignPriceRuleToProductVariantCommand(7, 8), CancellationToken.None);

        // Assert
        actual.Should().BeSameAs(result);
    }

    [Fact]
    public async Task RemoveHandler_ShouldReturnServiceResult()
    {
        // Arrange
        var catalogService = new Mock<IAdminCatalogService>();
        var result = new AdminCatalogOperationResult(AdminCatalogOperationStatus.Success);
        catalogService.Setup(candidate => candidate.RemovePriceRuleFromProductVariantAsync(7, 8, CancellationToken.None)).ReturnsAsync(result);
        var handler = new RemovePriceRuleFromProductVariantCommandHandler(catalogService.Object);

        // Act
        var actual = await handler.Handle(new RemovePriceRuleFromProductVariantCommand(7, 8), CancellationToken.None);

        // Assert
        actual.Should().BeSameAs(result);
    }
}