using FluentAssertions;
using FruitShop.Api.Features.AdminCatalog;
using FruitShop.Api.Models;
using FruitShop.Api.Services;
using Moq;

namespace FruitShop.Api.Tests;

public sealed class GetAdminCatalogQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnCatalog_WhenServiceCompletes()
    {
        // Arrange
        var catalogService = new Mock<IAdminCatalogService>();
        var pageRequest = new AdminCatalogPageRequest();
        var catalog = new AdminCatalogResponse(new PagedResult<AdminProductResponse>([], 1, 25, 0), [], new PagedResult<AdminPriceRuleResponse>([], 1, 25, 0));
        catalogService.Setup(candidate => candidate.GetCatalogAsync(pageRequest, CancellationToken.None)).ReturnsAsync(catalog);
        var handler = new GetAdminCatalogQueryHandler(catalogService.Object);

        // Act
        var result = await handler.Handle(new GetAdminCatalogQuery(pageRequest), CancellationToken.None);

        // Assert
        result.Should().BeSameAs(catalog);
        catalogService.Verify(candidate => candidate.GetCatalogAsync(pageRequest, CancellationToken.None), Times.Once);
    }
}