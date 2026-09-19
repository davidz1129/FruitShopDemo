using FluentAssertions;
using FruitShop.Api.Controllers;
using FruitShop.Api.Features.Products;
using FruitShop.Api.Models;
using MediatR;
using Moq;

namespace FruitShop.Api.Tests;

public sealed class ProductsControllerTests
{
    [Fact]
    public async Task GetAll_ShouldReturnPagedProducts_WhenSenderCompletes()
    {
        // Arrange
        var sender = new Mock<ISender>();
        var pageRequest = new PageRequest { Page = 2, PageSize = 10 };
        var response = new PagedResult<ProductResponse>([new ProductResponse(4, "Apples", [])], 2, 10, 11);
        sender.Setup(candidate => candidate.Send(It.Is<GetProductsQuery>(query => query.PageRequest == pageRequest), CancellationToken.None))
            .ReturnsAsync(response);
        var controller = new ProductsController(sender.Object);

        // Act
        var result = await controller.GetAll(pageRequest, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(response);
        sender.Verify(candidate => candidate.Send(It.Is<GetProductsQuery>(query => query.PageRequest == pageRequest), CancellationToken.None), Times.Once);
    }
}