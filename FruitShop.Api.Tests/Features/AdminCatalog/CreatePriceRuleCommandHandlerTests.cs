using FluentAssertions;
using FruitShop.Api.Features.AdminCatalog;
using FruitShop.Api.Models;
using FruitShop.Api.Services;
using Moq;

namespace FruitShop.Api.Tests;

public sealed class CreatePriceRuleCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnOperationResult_WhenServiceCompletes()
    {
        // Arrange
        var catalogService = new Mock<IAdminCatalogService>();
        var request = new CreatePriceRuleRequest
        {
            Name = "VIP discount",
            Priority = 1,
            IsStackable = true,
            AppliesToAllVariants = true,
            Actions = [new CreatePriceRuleActionRequest { ActionType = ActionType.AmountOff, Amount = 1m }]
        };
        var result = new AdminCatalogOperationResult<AdminPriceRuleResponse>(AdminCatalogOperationStatus.InvalidRule, null, "Invalid");
        catalogService.Setup(candidate => candidate.CreatePriceRuleAsync(request, CancellationToken.None)).ReturnsAsync(result);
        var handler = new CreatePriceRuleCommandHandler(catalogService.Object);

        // Act
        var actual = await handler.Handle(new CreatePriceRuleCommand(request), CancellationToken.None);

        // Assert
        actual.Should().BeSameAs(result);
        catalogService.Verify(candidate => candidate.CreatePriceRuleAsync(request, CancellationToken.None), Times.Once);
    }
}