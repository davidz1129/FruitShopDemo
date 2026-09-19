using AutoMapper;
using FruitShop.Api.Models;
using FruitShop.Api.Services;
using MediatR;

namespace FruitShop.Api.Features.Orders;

/// <summary>Payload for atomically creating and submitting an order.</summary>
public sealed record SubmitOrderRequest
{
    public required string CustomerTier { get; init; }

    public required IReadOnlyList<SubmitOrderItemRequest> Items { get; init; }
}

/// <summary>Represents one item in an atomic order submission.</summary>
public sealed record SubmitOrderItemRequest
{
    public required long VariantId { get; init; }

    public required decimal Quantity { get; init; }
}

public sealed record SubmitOrderCommand(string CustomerTier, IReadOnlyList<SubmitOrderItemRequest> Items)
    : IRequest<OrderResponse>;

public sealed class SubmitOrderCommandHandler(IOrderProcessingService orderProcessingService, IMapper mapper)
    : IRequestHandler<SubmitOrderCommand, OrderResponse>
{
    public async Task<OrderResponse> Handle(SubmitOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await orderProcessingService.CreateAndSubmitOrderAsync(
            request.CustomerTier,
            mapper.Map<List<OrderItemSubmission>>(request.Items),
            cancellationToken);

        return mapper.Map<OrderResponse>(order);
    }
}