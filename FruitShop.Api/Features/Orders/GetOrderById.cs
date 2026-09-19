using AutoMapper;
using FruitShop.Api.Repositories;
using MediatR;

namespace FruitShop.Api.Features.Orders;

public sealed record GetOrderByIdQuery(long Id) : IRequest<OrderResponse?>;

public sealed class GetOrderByIdQueryHandler(IOrderRepository repository, IMapper mapper)
    : IRequestHandler<GetOrderByIdQuery, OrderResponse?>
{
    public async Task<OrderResponse?> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await repository.GetByIdAsync(request.Id, cancellationToken);
        return order is null ? null : mapper.Map<OrderResponse>(order);
    }
}