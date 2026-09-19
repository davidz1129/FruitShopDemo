using AutoMapper;
using FluentAssertions;
using FruitShop.Api.Features.Orders;
using FruitShop.Api.Models;
using FruitShop.Api.Repositories;
using Moq;

namespace FruitShop.Api.Tests;

public sealed class GetOrderByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldMapOrder_WhenRepositoryFindsOrder()
    {
        // Arrange
        var repository = new Mock<IOrderRepository>();
        var mapper = new Mock<IMapper>();
        var order = new Order { Id = 17, CustomerTier = "VIP" };
        var response = CreateResponse(17);
        repository.Setup(candidate => candidate.GetByIdAsync(17, CancellationToken.None)).ReturnsAsync(order);
        mapper.Setup(candidate => candidate.Map<OrderResponse>(order)).Returns(response);
        var handler = new GetOrderByIdQueryHandler(repository.Object, mapper.Object);

        // Act
        var result = await handler.Handle(new GetOrderByIdQuery(17), CancellationToken.None);

        // Assert
        result.Should().BeSameAs(response);
        mapper.Verify(candidate => candidate.Map<OrderResponse>(order), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnNull_WhenRepositoryDoesNotFindOrder()
    {
        // Arrange
        var repository = new Mock<IOrderRepository>();
        var mapper = new Mock<IMapper>();
        repository.Setup(candidate => candidate.GetByIdAsync(42, CancellationToken.None)).ReturnsAsync((Order?)null);
        var handler = new GetOrderByIdQueryHandler(repository.Object, mapper.Object);

        // Act
        var result = await handler.Handle(new GetOrderByIdQuery(42), CancellationToken.None);

        // Assert
        result.Should().BeNull();
        mapper.Verify(candidate => candidate.Map<OrderResponse>(It.IsAny<Order>()), Times.Never);
    }

    private static OrderResponse CreateResponse(long id) =>
        new(id, "VIP", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, OrderStatus.Pending, 0m, []);
}