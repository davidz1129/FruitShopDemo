using FluentAssertions;
using FruitShop.Api.Features.AdminCatalog;
using FruitShop.Api.Models;
using FruitShop.Api.Services;
using Moq;

namespace FruitShop.Api.Tests;

public sealed class SetProductVariantStatusCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnOperationResult_WhenServiceCompletes()
    {
        // Arrange
        var catalogService = new Mock<IAdminCatalogService>();
        var request = new SetProductVariantStatusRequest { IsActive = false };
        var result = new AdminCatalogOperationResult<AdminProductVariantResponse>(AdminCatalogOperationStatus.VariantNotFound, null);
        catalogService.Setup(candidate => candidate.SetVariantStatusAsync(6, request, CancellationToken.None)).ReturnsAsync(result);
        var handler = new SetProductVariantStatusCommandHandler(catalogService.Object);

        // Act
        var actual = await handler.Handle(new SetProductVariantStatusCommand(6, request), CancellationToken.None);

        // Assert
        actual.Should().BeSameAs(result);
        catalogService.Verify(candidate => candidate.SetVariantStatusAsync(6, request, CancellationToken.None), Times.Once);
    }
}