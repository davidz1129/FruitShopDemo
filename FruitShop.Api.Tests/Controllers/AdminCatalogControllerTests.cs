using FluentAssertions;
using FruitShop.Api.Controllers;
using FruitShop.Api.Features.AdminCatalog;
using FruitShop.Api.Models;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Moq;

namespace FruitShop.Api.Tests;

public sealed class AdminCatalogControllerTests
{
    [Fact]
    public async Task GetCatalog_ShouldReturnCatalog_WhenSenderCompletes()
    {
        // Arrange
        var sender = new Mock<ISender>();
        var pageRequest = new AdminCatalogPageRequest();
        var catalog = EmptyCatalog();
        sender.Setup(candidate => candidate.Send(It.Is<GetAdminCatalogQuery>(query => query.PageRequest == pageRequest), CancellationToken.None))
            .ReturnsAsync(catalog);
        var controller = new AdminCatalogController(sender.Object);

        // Act
        var result = await controller.GetCatalog(pageRequest, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(catalog);
        sender.Verify(candidate => candidate.Send(It.Is<GetAdminCatalogQuery>(query => query.PageRequest == pageRequest), CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task CreateProduct_ShouldReturnCreated_WhenSenderCreatesProduct()
    {
        // Arrange
        var sender = new Mock<ISender>();
        var request = new CreateAdminProductRequest { Name = "Apples" };
        var product = new AdminProductResponse(4, "Apples", []);
        sender.Setup(candidate => candidate.Send(new CreateAdminProductCommand(request), CancellationToken.None)).ReturnsAsync(product);
        var controller = new AdminCatalogController(sender.Object);

        // Act
        var result = await controller.CreateProduct(request, CancellationToken.None);

        // Assert
        var created = result.Result.Should().BeOfType<CreatedResult>().Subject;
        created.Location.Should().Be("/api/admin/products/4");
        created.Value.Should().BeSameAs(product);
    }

    [Theory]
    [InlineData(AdminCatalogOperationStatus.ProductNotFound, StatusCodes.Status404NotFound)]
    [InlineData(AdminCatalogOperationStatus.UnitOfMeasureNotFound, StatusCodes.Status400BadRequest)]
    [InlineData(AdminCatalogOperationStatus.DuplicateSku, StatusCodes.Status400BadRequest)]
    public async Task CreateVariant_ShouldReturnExpectedError_WhenOperationFails(AdminCatalogOperationStatus status, int expectedStatusCode)
    {
        // Arrange
        var sender = new Mock<ISender>();
        var request = CreateVariantRequest();
        var operation = new AdminCatalogOperationResult<AdminProductVariantResponse>(status, null, "Invalid variant");
        sender.Setup(candidate => candidate.Send(new CreateProductVariantCommand(6, request), CancellationToken.None)).ReturnsAsync(operation);
        var controller = new AdminCatalogController(sender.Object);

        // Act
        var result = await controller.CreateVariant(6, request, CancellationToken.None);

        // Assert
        if (expectedStatusCode == StatusCodes.Status400BadRequest)
        {
            AssertValidationProblem(result.Result!);
        }
        else
        {
            result.Result.Should().BeAssignableTo<IStatusCodeActionResult>().Which.StatusCode.Should().Be(expectedStatusCode);
        }
        sender.Verify(candidate => candidate.Send(new CreateProductVariantCommand(6, request), CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task CreateVariant_ShouldReturnCreated_WhenOperationSucceeds()
    {
        // Arrange
        var sender = new Mock<ISender>();
        var request = CreateVariantRequest();
        var response = new AdminProductVariantResponse(7, "APPLE", "kg", 1m, 2m, true, MeasureType.Weight);
        var operation = new AdminCatalogOperationResult<AdminProductVariantResponse>(AdminCatalogOperationStatus.Success, response);
        sender.Setup(candidate => candidate.Send(new CreateProductVariantCommand(6, request), CancellationToken.None)).ReturnsAsync(operation);
        var controller = new AdminCatalogController(sender.Object);

        // Act
        var result = await controller.CreateVariant(6, request, CancellationToken.None);

        // Assert
        var created = result.Result.Should().BeOfType<CreatedResult>().Subject;
        created.Location.Should().Be("/api/admin/variants/7");
        created.Value.Should().BeSameAs(response);
    }

    [Theory]
    [InlineData(AdminCatalogOperationStatus.VariantNotFound, StatusCodes.Status404NotFound)]
    [InlineData(AdminCatalogOperationStatus.Success, StatusCodes.Status200OK)]
    public async Task SetVariantStatus_ShouldReturnExpectedStatus_WhenOperationCompletes(AdminCatalogOperationStatus status, int expectedStatusCode)
    {
        // Arrange
        var sender = new Mock<ISender>();
        var request = new SetProductVariantStatusRequest { IsActive = false };
        var response = new AdminProductVariantResponse(7, "APPLE", "kg", 1m, 2m, false, MeasureType.Weight);
        var operation = new AdminCatalogOperationResult<AdminProductVariantResponse>(status, status == AdminCatalogOperationStatus.Success ? response : null);
        sender.Setup(candidate => candidate.Send(new SetProductVariantStatusCommand(7, request), CancellationToken.None)).ReturnsAsync(operation);
        var controller = new AdminCatalogController(sender.Object);

        // Act
        var result = await controller.SetVariantStatus(7, request, CancellationToken.None);

        // Assert
        result.Result.Should().BeAssignableTo<IStatusCodeActionResult>().Which.StatusCode.Should().Be(expectedStatusCode);
    }

    [Fact]
    public async Task GetProductVariantPriceRules_ShouldReturnNotFound_WhenVariantIsMissing()
    {
        // Arrange
        var sender = new Mock<ISender>();
        sender.Setup(candidate => candidate.Send(new GetProductVariantPriceRulesQuery(7), CancellationToken.None))
            .ReturnsAsync(new AdminCatalogOperationResult<ProductVariantPriceRulesResponse>(AdminCatalogOperationStatus.VariantNotFound, null));
        var controller = new AdminCatalogController(sender.Object);

        // Act
        var result = await controller.GetProductVariantPriceRules(7, CancellationToken.None);

        // Assert
        result.Result.Should().BeAssignableTo<IStatusCodeActionResult>().Which.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task AssignPriceRuleToProductVariant_ShouldReturnValidationProblem_WhenVariantIsInactive()
    {
        // Arrange
        var sender = new Mock<ISender>();
        sender.Setup(candidate => candidate.Send(new AssignPriceRuleToProductVariantCommand(7, 8), CancellationToken.None))
            .ReturnsAsync(new AdminCatalogOperationResult<AdminPriceRuleResponse>(AdminCatalogOperationStatus.VariantInactive, null, "Variant is inactive."));
        var controller = new AdminCatalogController(sender.Object);

        // Act
        var result = await controller.AssignPriceRuleToProductVariant(7, 8, CancellationToken.None);

        // Assert
        AssertValidationProblem(result.Result!);
    }

    [Fact]
    public async Task RemovePriceRuleFromProductVariant_ShouldReturnNoContent_WhenTargetIsRemoved()
    {
        // Arrange
        var sender = new Mock<ISender>();
        sender.Setup(candidate => candidate.Send(new RemovePriceRuleFromProductVariantCommand(7, 8), CancellationToken.None))
            .ReturnsAsync(new AdminCatalogOperationResult(AdminCatalogOperationStatus.Success));
        var controller = new AdminCatalogController(sender.Object);

        // Act
        var result = await controller.RemovePriceRuleFromProductVariant(7, 8, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Theory]
    [InlineData(AdminCatalogOperationStatus.InvalidRule, StatusCodes.Status400BadRequest)]
    [InlineData(AdminCatalogOperationStatus.Success, StatusCodes.Status201Created)]
    public async Task CreatePriceRule_ShouldReturnExpectedStatus_WhenOperationCompletes(AdminCatalogOperationStatus status, int expectedStatusCode)
    {
        // Arrange
        var sender = new Mock<ISender>();
        var request = CreatePriceRuleRequest();
        var response = new AdminPriceRuleResponse(8, "VIP discount", 1, true, true, RuleStatus.Active, [], [], [], []);
        var operation = new AdminCatalogOperationResult<AdminPriceRuleResponse>(status, status == AdminCatalogOperationStatus.Success ? response : null, "Invalid rule");
        sender.Setup(candidate => candidate.Send(new CreatePriceRuleCommand(request), CancellationToken.None)).ReturnsAsync(operation);
        var controller = new AdminCatalogController(sender.Object);

        // Act
        var result = await controller.CreatePriceRule(request, CancellationToken.None);

        // Assert
        var actionResult = result.Result!;
        if (expectedStatusCode == StatusCodes.Status400BadRequest)
        {
            AssertValidationProblem(actionResult);
        }
        else
        {
            actionResult.Should().BeAssignableTo<IStatusCodeActionResult>().Which.StatusCode.Should().Be(expectedStatusCode);
        }
        if (status == AdminCatalogOperationStatus.Success)
        {
            actionResult.Should().BeOfType<CreatedResult>().Which.Location.Should().Be("/api/admin/price-rules/8");
        }
    }

    private static AdminCatalogResponse EmptyCatalog() =>
        new(new PagedResult<AdminProductResponse>([], 1, 25, 0), [], new PagedResult<AdminPriceRuleResponse>([], 1, 25, 0));

    private static CreateProductVariantRequest CreateVariantRequest() =>
        new() { Sku = "APPLE", UomCode = "kg", UomFactor = 1m, BasePrice = 2m };

    private static CreatePriceRuleRequest CreatePriceRuleRequest() =>
        new()
        {
            Name = "VIP discount",
            Priority = 1,
            IsStackable = true,
            AppliesToAllVariants = true,
            Actions = [new CreatePriceRuleActionRequest { ActionType = ActionType.AmountOff, Amount = 1m }]
        };

    private static void AssertValidationProblem(IActionResult actionResult)
    {
        if (actionResult is ObjectResult { Value: ValidationProblemDetails problemDetails })
        {
            problemDetails.Errors.Should().ContainKey("request");
            return;
        }

        throw new Xunit.Sdk.XunitException("Expected a validation problem response.");
    }
}