using AutoMapper;
using FluentAssertions;
using FruitShop.Api.Features.Orders;
using FruitShop.Api.Models;
using FruitShop.Api.Repositories;
using Moq;

namespace FruitShop.Api.Tests;

public sealed class GetOrdersQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldMapPagedOrders_WhenRepositoryReturnsOrders()
    {
        // Arrange
        var repository = new Mock<IOrderRepository>();
        var mapper = new Mock<IMapper>();
        var pageRequest = new PageRequest { Page = 3, PageSize = 2 };
        var orders = new List<Order> { new() { Id = 7, CustomerTier = "VIP" } };
        var responses = new List<OrderListItemResponse> { new(7, DateTimeOffset.UtcNow, OrderStatus.Pending) };
        repository.Setup(candidate => candidate.GetAllAsync(pageRequest, CancellationToken.None))
            .ReturnsAsync(new PagedResult<Order>(orders, 3, 2, 9));
        mapper.Setup(candidate => candidate.Map<List<OrderListItemResponse>>(orders)).Returns(responses);
        var handler = new GetOrdersQueryHandler(repository.Object, mapper.Object);

        // Act
        var result = await handler.Handle(new GetOrdersQuery(pageRequest), CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(new PagedResult<OrderListItemResponse>(responses, 3, 2, 9));
        repository.Verify(candidate => candidate.GetAllAsync(pageRequest, CancellationToken.None), Times.Once);
        mapper.Verify(candidate => candidate.Map<List<OrderListItemResponse>>(orders), Times.Once);
    }
}