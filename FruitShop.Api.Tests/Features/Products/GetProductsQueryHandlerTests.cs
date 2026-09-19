using AutoMapper;
using FluentAssertions;
using FruitShop.Api.Features.Products;
using FruitShop.Api.Models;
using FruitShop.Api.Repositories;
using Moq;

namespace FruitShop.Api.Tests;

public sealed class GetProductsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldMapPagedProducts_WhenRepositoryReturnsProducts()
    {
        // Arrange
        var repository = new Mock<IProductRepository>();
        var mapper = new Mock<IMapper>();
        var pageRequest = new PageRequest { Page = 2, PageSize = 5 };
        var products = new List<Product> { new() { Id = 1, Name = "Apples" } };
        var responses = new List<ProductResponse> { new(1, "Apples", []) };
        repository.Setup(candidate => candidate.GetProductsAsync(pageRequest, CancellationToken.None))
            .ReturnsAsync(new PagedResult<Product>(products, 2, 5, 11));
        mapper.Setup(candidate => candidate.Map<List<ProductResponse>>(products)).Returns(responses);
        var handler = new GetProductsQueryHandler(repository.Object, mapper.Object);

        // Act
        var result = await handler.Handle(new GetProductsQuery(pageRequest), CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(new PagedResult<ProductResponse>(responses, 2, 5, 11));
        repository.Verify(candidate => candidate.GetProductsAsync(pageRequest, CancellationToken.None), Times.Once);
        mapper.Verify(candidate => candidate.Map<List<ProductResponse>>(products), Times.Once);
    }
}