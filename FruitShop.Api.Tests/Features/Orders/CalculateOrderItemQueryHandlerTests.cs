using AutoMapper;
using FluentAssertions;
using FruitShop.Api.Features.Orders;
using FruitShop.Api.Models;
using FruitShop.Api.Services;
using Moq;

namespace FruitShop.Api.Tests;

public sealed class CalculateOrderItemQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldMapPreview_WhenCalculationServiceReturnsPreview()
    {
        // Arrange
        var calculationService = new Mock<IOrderItemCalculationService>();
        var mapper = new Mock<IMapper>();
        var preview = CreatePreview();
        var response = new CalculateOrderItemResponse(4, "APPLE", "kg", 4m, 2m, 3m, 6m, "Promotion", []);
        calculationService.Setup(candidate => candidate.CalculateAsync(4, "VIP", 2m, 20m, CancellationToken.None))
            .ReturnsAsync(preview);
        mapper.Setup(candidate => candidate.Map<CalculateOrderItemResponse>(preview)).Returns(response);
        var handler = new CalculateOrderItemQueryHandler(calculationService.Object, mapper.Object);

        // Act
        var result = await handler.Handle(new CalculateOrderItemQuery(4, "VIP", 2m, 20m), CancellationToken.None);

        // Assert
        result.Should().BeSameAs(response);
        mapper.Verify(candidate => candidate.Map<CalculateOrderItemResponse>(preview), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnNull_WhenCalculationServiceReturnsNull()
    {
        // Arrange
        var calculationService = new Mock<IOrderItemCalculationService>();
        var mapper = new Mock<IMapper>();
        calculationService.Setup(candidate => candidate.CalculateAsync(5, "RETAIL", 1m, 0m, CancellationToken.None))
            .ReturnsAsync((OrderItemPreview?)null);
        var handler = new CalculateOrderItemQueryHandler(calculationService.Object, mapper.Object);

        // Act
        var result = await handler.Handle(new CalculateOrderItemQuery(5, "RETAIL", 1m, 0m), CancellationToken.None);

        // Assert
        result.Should().BeNull();
        mapper.Verify(candidate => candidate.Map<CalculateOrderItemResponse>(It.IsAny<OrderItemPreview>()), Times.Never);
    }

    private static OrderItemPreview CreatePreview() =>
        new(
            new ProductVariant { Id = 4, Sku = "APPLE", UomCode = "kg", BasePrice = 4m },
            2m,
            new PriceCalculation(3m, "Promotion"),
            []);
}