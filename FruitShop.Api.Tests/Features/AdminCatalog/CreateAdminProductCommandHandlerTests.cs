using FluentAssertions;
using FruitShop.Api.Features.AdminCatalog;
using FruitShop.Api.Models;
using FruitShop.Api.Services;
using Moq;

namespace FruitShop.Api.Tests;

public sealed class CreateAdminProductCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnProduct_WhenServiceCreatesProduct()
    {
        // Arrange
        var catalogService = new Mock<IAdminCatalogService>();
        var request = new CreateAdminProductRequest { Name = "Apples" };
        var product = new AdminProductResponse(9, "Apples", []);
        catalogService.Setup(candidate => candidate.CreateProductAsync(request, CancellationToken.None)).ReturnsAsync(product);
        var handler = new CreateAdminProductCommandHandler(catalogService.Object);

        // Act
        var result = await handler.Handle(new CreateAdminProductCommand(request), CancellationToken.None);

        // Assert
        result.Should().BeSameAs(product);
        catalogService.Verify(candidate => candidate.CreateProductAsync(request, CancellationToken.None), Times.Once);
    }
}