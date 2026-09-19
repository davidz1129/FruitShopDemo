using AutoMapper;
using FruitShop.Api.Models;
using FruitShop.Api.Repositories;
using MediatR;

namespace FruitShop.Api.Features.Orders;

public sealed record GetOrdersQuery(PageRequest PageRequest) : IRequest<PagedResult<OrderListItemResponse>>;

public sealed class GetOrdersQueryHandler(IOrderRepository repository, IMapper mapper)
    : IRequestHandler<GetOrdersQuery, PagedResult<OrderListItemResponse>>
{
    public async Task<PagedResult<OrderListItemResponse>> Handle(
        GetOrdersQuery request,
        CancellationToken cancellationToken)
    {
        var orders = await repository.GetAllAsync(request.PageRequest, cancellationToken);
        return new PagedResult<OrderListItemResponse>(
            mapper.Map<List<OrderListItemResponse>>(orders.Items),
            orders.Page,
            orders.PageSize,
            orders.TotalCount);
    }
}

/// <summary>Represents an order in an order list.</summary>
public sealed record OrderListItemResponse(long Id, DateTimeOffset CreatedAt, OrderStatus Status);