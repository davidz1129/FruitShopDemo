using AutoMapper;
using FluentAssertions;
using FruitShop.Api.Features.Orders;
using FruitShop.Api.Models;
using FruitShop.Api.Services;
using Moq;

namespace FruitShop.Api.Tests;

public sealed class SubmitOrderCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldSubmitMappedItemsAndMapOrder_WhenRequestIsValid()
    {
        // Arrange
        var processingService = new Mock<IOrderProcessingService>();
        var mapper = new Mock<IMapper>();
        var items = new List<SubmitOrderItemRequest> { new() { VariantId = 3, Quantity = 2m } };
        var submissions = new List<OrderItemSubmission> { new(3, 2m) };
        var order = new Order { Id = 12, CustomerTier = "VIP" };
        var response = new OrderResponse(12, "VIP", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, OrderStatus.Submitted, 0m, []);
        mapper.Setup(candidate => candidate.Map<List<OrderItemSubmission>>(items)).Returns(submissions);
        processingService.Setup(candidate => candidate.CreateAndSubmitOrderAsync("VIP", submissions, CancellationToken.None))
            .ReturnsAsync(order);
        mapper.Setup(candidate => candidate.Map<OrderResponse>(order)).Returns(response);
        var handler = new SubmitOrderCommandHandler(processingService.Object, mapper.Object);

        // Act
        var result = await handler.Handle(new SubmitOrderCommand("VIP", items), CancellationToken.None);

        // Assert
        result.Should().BeSameAs(response);
        processingService.Verify(candidate => candidate.CreateAndSubmitOrderAsync("VIP", submissions, CancellationToken.None), Times.Once);
        mapper.Verify(candidate => candidate.Map<OrderResponse>(order), Times.Once);
    }
}