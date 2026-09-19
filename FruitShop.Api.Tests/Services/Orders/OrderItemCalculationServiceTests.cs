using FluentAssertions;
using FruitShop.Api.Models;
using FruitShop.Api.Repositories;
using FruitShop.Api.Services;
using Moq;

namespace FruitShop.Api.Tests;

public sealed class OrderItemCalculationServiceTests
{
    [Fact]
    public async Task QuoteAsync_ShouldNormalizeTierAndCalculatePrice_WhenRulesAreAvailable()
    {
        // Arrange
        var productRepository = new Mock<IProductRepository>();
        var ruleRepository = new Mock<IPriceRuleRepository>();
        var pricingService = new Mock<IPricingService>();
        var variant = new ProductVariant { Id = 4, Sku = "APPLE", UomCode = "kg", BasePrice = 5m };
        var rules = new List<PriceRule> { new() { Name = "VIP" } };
        var calculation = new PriceCalculation(4m, "VIP discount");
        var request = new OrderItemQuoteRequest(variant, " vip ", 2m, 12m, new DateTime(2026, 9, 19));
        ruleRepository.Setup(candidate => candidate.GetActiveRulesAsync(4, "VIP", CancellationToken.None)).ReturnsAsync(rules);
        pricingService.Setup(candidate => candidate.CalculatePrice(
            It.Is<PricingContext>(context => context.VariantId == 4 && context.CustomerTier == "VIP" && context.Quantity == 2m && context.CartSubtotal == 12m),
            rules)).Returns(calculation);
        var service = new OrderItemCalculationService(productRepository.Object, ruleRepository.Object, pricingService.Object);

        // Act
        var result = await service.QuoteAsync(request);

        // Assert
        result.Should().BeEquivalentTo(new OrderItemQuote(calculation, rules));
        ruleRepository.Verify(candidate => candidate.GetActiveRulesAsync(4, "VIP", CancellationToken.None), Times.Once);
        pricingService.Verify(candidate => candidate.CalculatePrice(It.IsAny<PricingContext>(), rules), Times.Once);
    }

    [Fact]
    public async Task CalculateAsync_ShouldReturnNull_WhenVariantIsMissing()
    {
        // Arrange
        var productRepository = new Mock<IProductRepository>();
        var ruleRepository = new Mock<IPriceRuleRepository>();
        var pricingService = new Mock<IPricingService>();
        productRepository.Setup(candidate => candidate.GetVariantByIdAsync(99, CancellationToken.None)).ReturnsAsync((ProductVariant?)null);
        var service = new OrderItemCalculationService(productRepository.Object, ruleRepository.Object, pricingService.Object);

        // Act
        var result = await service.CalculateAsync(99, "VIP", 1m, 0m);

        // Assert
        result.Should().BeNull();
        ruleRepository.Verify(candidate => candidate.GetActiveRulesAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CalculateAsync_ShouldReturnPreview_WhenVariantExists()
    {
        // Arrange
        var productRepository = new Mock<IProductRepository>();
        var ruleRepository = new Mock<IPriceRuleRepository>();
        var pricingService = new Mock<IPricingService>();
        var variant = new ProductVariant { Id = 3, Sku = "PEAR", UomCode = "kg", BasePrice = 6m };
        var rules = new List<PriceRule>();
        var calculation = new PriceCalculation(5m, "Promotion");
        productRepository.Setup(candidate => candidate.GetVariantByIdAsync(3, CancellationToken.None)).ReturnsAsync(variant);
        ruleRepository.Setup(candidate => candidate.GetActiveRulesAsync(3, "RETAIL", CancellationToken.None)).ReturnsAsync(rules);
        pricingService.Setup(candidate => candidate.CalculatePrice(It.IsAny<PricingContext>(), rules)).Returns(calculation);
        var service = new OrderItemCalculationService(productRepository.Object, ruleRepository.Object, pricingService.Object);

        // Act
        var result = await service.CalculateAsync(3, "RETAIL", 2m, 7m);

        // Assert
        result.Should().BeEquivalentTo(new OrderItemPreview(variant, 2m, calculation, rules));
        productRepository.Verify(candidate => candidate.GetVariantByIdAsync(3, CancellationToken.None), Times.Once);
    }
}