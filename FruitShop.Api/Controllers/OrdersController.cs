using AutoMapper;
using FruitShop.Api.Features.Orders;
using FruitShop.Api.Models;
using FruitShop.Api.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FruitShop.Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController(ISender sender, IMapper mapper) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<OrderListItemResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<OrderListItemResponse>>> GetAll(
        [FromQuery] PageRequest pageRequest,
        CancellationToken cancellationToken)
    {
        var orders = await sender.Send(new GetOrdersQuery(pageRequest), cancellationToken);
        return Ok(orders);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<OrderResponse>> GetById(long id, CancellationToken cancellationToken)
    {
        var order = await sender.Send(new GetOrderByIdQuery(id), cancellationToken);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpPost("submit")]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponse>> CreateAndSubmit(
        SubmitOrderRequest request,
        CancellationToken cancellationToken)
    {
        var order = await sender.Send(mapper.Map<SubmitOrderCommand>(request), cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    [HttpPost("calculate-item")]
    [ProducesResponseType<CalculateOrderItemResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CalculateOrderItemResponse>> CalculateItem(
        CalculateOrderItemRequest request,
        CancellationToken cancellationToken)
    {
        var preview = await sender.Send(mapper.Map<CalculateOrderItemQuery>(request), cancellationToken);

        return preview is null ? NotFound() : Ok(preview);
    }
}