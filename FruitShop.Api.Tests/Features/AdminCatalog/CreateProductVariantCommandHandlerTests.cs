using FluentAssertions;
using FruitShop.Api.Features.AdminCatalog;
using FruitShop.Api.Models;
using FruitShop.Api.Services;
using Moq;

namespace FruitShop.Api.Tests;

public sealed class CreateProductVariantCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnOperationResult_WhenServiceCompletes()
    {
        // Arrange
        var catalogService = new Mock<IAdminCatalogService>();
        var request = new CreateProductVariantRequest { Sku = "APPLE", UomCode = "kg", UomFactor = 1m, BasePrice = 2m };
        var result = new AdminCatalogOperationResult<AdminProductVariantResponse>(
            AdminCatalogOperationStatus.Success,
            new AdminProductVariantResponse(2, "APPLE", "kg", 1m, 2m, true, MeasureType.Weight));
        catalogService.Setup(candidate => candidate.CreateVariantAsync(8, request, CancellationToken.None)).ReturnsAsync(result);
        var handler = new CreateProductVariantCommandHandler(catalogService.Object);

        // Act
        var actual = await handler.Handle(new CreateProductVariantCommand(8, request), CancellationToken.None);

        // Assert
        actual.Should().BeSameAs(result);
        catalogService.Verify(candidate => candidate.CreateVariantAsync(8, request, CancellationToken.None), Times.Once);
    }
}