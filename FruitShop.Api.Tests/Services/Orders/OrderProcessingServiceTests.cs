using FluentAssertions;
using FruitShop.Api.Models;
using FruitShop.Api.Repositories;
using FruitShop.Api.Services;
using Moq;

namespace FruitShop.Api.Tests;

public sealed class OrderProcessingServiceTests
{
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task CreateAndSubmitOrderAsync_ShouldThrowArgumentException_WhenCustomerTierIsBlank(string customerTier)
    {
        // Arrange
        var service = new OrderProcessingService(null!, Mock.Of<IOrderRepository>(), Mock.Of<IProductRepository>(), Mock.Of<IOrderItemCalculationService>());

        // Act
        Func<Task> act = () => service.CreateAndSubmitOrderAsync(customerTier, []);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateAndSubmitOrderAsync_ShouldThrowArgumentException_WhenItemsAreEmpty()
    {
        // Arrange
        var service = new OrderProcessingService(null!, Mock.Of<IOrderRepository>(), Mock.Of<IProductRepository>(), Mock.Of<IOrderItemCalculationService>());

        // Act
        Func<Task> act = () => service.CreateAndSubmitOrderAsync("VIP", []);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateAndSubmitOrderAsync_ShouldAddQuoteAndSubmitOrder_WhenDependenciesSucceed()
    {
        // Arrange
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var dbContext = database.CreateContext();
        var orderRepository = new Mock<IOrderRepository>();
        var productRepository = new Mock<IProductRepository>();
        var calculationService = new Mock<IOrderItemCalculationService>();
        var order = new Order { Id = 14, CustomerTier = "VIP" };
        var variant = new ProductVariant { Id = 8, Sku = "APPLE", UomCode = "kg", BasePrice = 5m };
        var quote = new OrderItemQuote(new PriceCalculation(3m, "VIP discount"), []);
        orderRepository.Setup(candidate => candidate.AddAsync(It.IsAny<Order>(), CancellationToken.None)).ReturnsAsync(order);
        orderRepository.SetupSequence(candidate => candidate.GetByIdAsync(order.Id, CancellationToken.None))
            .ReturnsAsync(order)
            .ReturnsAsync(order);
        productRepository.Setup(candidate => candidate.GetVariantByIdAsync(variant.Id, CancellationToken.None)).ReturnsAsync(variant);
        calculationService.Setup(candidate => candidate.QuoteAsync(It.IsAny<OrderItemQuoteRequest>(), CancellationToken.None)).ReturnsAsync(quote);
        var service = new OrderProcessingService(dbContext, orderRepository.Object, productRepository.Object, calculationService.Object);

        // Act
        var result = await service.CreateAndSubmitOrderAsync("VIP", [new OrderItemSubmission(variant.Id, 2m)]);

        // Assert
        result.Should().BeSameAs(order);
        result.Status.Should().Be(OrderStatus.Submitted);
        result.TotalAmount.Should().Be(6m);
        result.Items.Should().ContainSingle().Which.PriceChangeReason.Should().Be("VIP discount");
        calculationService.Verify(candidate => candidate.QuoteAsync(It.Is<OrderItemQuoteRequest>(request =>
            request.Variant == variant && request.CustomerTier == "VIP" && request.Quantity == 2m && request.CartSubtotal == 10m), CancellationToken.None), Times.Once);
        orderRepository.Verify(candidate => candidate.SaveChangesAsync(CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task CreateAndSubmitOrderAsync_ShouldRollbackAndRethrow_WhenPriceCalculationFails()
    {
        // Arrange
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var dbContext = database.CreateContext();
        var orderRepository = new Mock<IOrderRepository>();
        var productRepository = new Mock<IProductRepository>();
        var calculationService = new Mock<IOrderItemCalculationService>();
        var order = new Order { Id = 14, CustomerTier = "VIP" };
        var variant = new ProductVariant { Id = 8, Sku = "APPLE", UomCode = "kg", BasePrice = 5m };
        orderRepository.Setup(candidate => candidate.AddAsync(It.IsAny<Order>(), CancellationToken.None)).ReturnsAsync(order);
        orderRepository.Setup(candidate => candidate.GetByIdAsync(order.Id, CancellationToken.None)).ReturnsAsync(order);
        productRepository.Setup(candidate => candidate.GetVariantByIdAsync(variant.Id, CancellationToken.None)).ReturnsAsync(variant);
        calculationService.Setup(candidate => candidate.QuoteAsync(It.IsAny<OrderItemQuoteRequest>(), CancellationToken.None))
            .ThrowsAsync(new InvalidOperationException("Pricing failed"));
        var service = new OrderProcessingService(dbContext, orderRepository.Object, productRepository.Object, calculationService.Object);

        // Act
        Func<Task> act = () => service.CreateAndSubmitOrderAsync("VIP", [new OrderItemSubmission(variant.Id, 2m)]);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Pricing failed");
        orderRepository.Verify(candidate => candidate.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}