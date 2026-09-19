using AutoMapper;
using FluentAssertions;
using FruitShop.Api.Controllers;
using FruitShop.Api.Features.Orders;
using FruitShop.Api.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FruitShop.Api.Tests;

public sealed class OrdersControllerTests
{
    [Fact]
    public async Task GetAll_ShouldReturnOkWithOrders_WhenSenderCompletes()
    {
        // Arrange
        var sender = new Mock<ISender>();
        var mapper = new Mock<IMapper>();
        var pageRequest = new PageRequest();
        var response = new PagedResult<OrderListItemResponse>([new OrderListItemResponse(1, DateTimeOffset.UtcNow, OrderStatus.Pending)], 1, 25, 1);
        sender.Setup(candidate => candidate.Send(It.Is<GetOrdersQuery>(query => query.PageRequest == pageRequest), CancellationToken.None))
            .ReturnsAsync(response);
        var controller = new OrdersController(sender.Object, mapper.Object);

        // Act
        var result = await controller.GetAll(pageRequest, CancellationToken.None);

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(response);
        sender.Verify(candidate => candidate.Send(It.Is<GetOrdersQuery>(query => query.PageRequest == pageRequest), CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenSenderReturnsNull()
    {
        // Arrange
        var sender = new Mock<ISender>();
        var mapper = new Mock<IMapper>();
        sender.Setup(candidate => candidate.Send(It.Is<GetOrderByIdQuery>(query => query.Id == 12), CancellationToken.None))
            .ReturnsAsync((OrderResponse?)null);
        var controller = new OrdersController(sender.Object, mapper.Object);

        // Act
        var result = await controller.GetById(12, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetById_ShouldReturnOkWithOrder_WhenSenderReturnsOrder()
    {
        // Arrange
        var sender = new Mock<ISender>();
        var mapper = new Mock<IMapper>();
        var response = CreateOrderResponse(12);
        sender.Setup(candidate => candidate.Send(It.Is<GetOrderByIdQuery>(query => query.Id == 12), CancellationToken.None))
            .ReturnsAsync(response);
        var controller = new OrdersController(sender.Object, mapper.Object);

        // Act
        var result = await controller.GetById(12, CancellationToken.None);

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(response);
    }

    [Fact]
    public async Task CreateAndSubmit_ShouldReturnCreatedAtAction_WhenSenderCreatesOrder()
    {
        // Arrange
        var sender = new Mock<ISender>();
        var mapper = new Mock<IMapper>();
        var request = new SubmitOrderRequest { CustomerTier = "VIP", Items = [new SubmitOrderItemRequest { VariantId = 2, Quantity = 1m }] };
        var command = new SubmitOrderCommand("VIP", request.Items);
        var response = CreateOrderResponse(33);
        mapper.Setup(candidate => candidate.Map<SubmitOrderCommand>(request)).Returns(command);
        sender.Setup(candidate => candidate.Send(command, CancellationToken.None)).ReturnsAsync(response);
        var controller = new OrdersController(sender.Object, mapper.Object);

        // Act
        var result = await controller.CreateAndSubmit(request, CancellationToken.None);

        // Assert
        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(OrdersController.GetById));
        created.RouteValues!["id"].Should().Be(33L);
        created.Value.Should().BeSameAs(response);
        mapper.Verify(candidate => candidate.Map<SubmitOrderCommand>(request), Times.Once);
        sender.Verify(candidate => candidate.Send(command, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task CalculateItem_ShouldReturnNotFound_WhenSenderReturnsNull()
    {
        // Arrange
        var sender = new Mock<ISender>();
        var mapper = new Mock<IMapper>();
        var request = new CalculateOrderItemRequest { VariantId = 9, CustomerTier = "RETAIL", Quantity = 1m, CartSubtotal = 0m };
        var command = new CalculateOrderItemQuery(9, "RETAIL", 1m, 0m);
        mapper.Setup(candidate => candidate.Map<CalculateOrderItemQuery>(request)).Returns(command);
        sender.Setup(candidate => candidate.Send(command, CancellationToken.None)).ReturnsAsync((CalculateOrderItemResponse?)null);
        var controller = new OrdersController(sender.Object, mapper.Object);

        // Act
        var result = await controller.CalculateItem(request, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task CalculateItem_ShouldReturnOkWithPreview_WhenSenderReturnsPreview()
    {
        // Arrange
        var sender = new Mock<ISender>();
        var mapper = new Mock<IMapper>();
        var request = new CalculateOrderItemRequest { VariantId = 9, CustomerTier = "RETAIL", Quantity = 1m, CartSubtotal = 0m };
        var command = new CalculateOrderItemQuery(9, "RETAIL", 1m, 0m);
        var response = new CalculateOrderItemResponse(9, "APPLE", "kg", 3m, 1m, 2m, 2m, "Discount", []);
        mapper.Setup(candidate => candidate.Map<CalculateOrderItemQuery>(request)).Returns(command);
        sender.Setup(candidate => candidate.Send(command, CancellationToken.None)).ReturnsAsync(response);
        var controller = new OrdersController(sender.Object, mapper.Object);

        // Act
        var result = await controller.CalculateItem(request, CancellationToken.None);

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(response);
    }

    private static OrderResponse CreateOrderResponse(long id) =>
        new(id, "VIP", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, OrderStatus.Submitted, 0m, []);
}