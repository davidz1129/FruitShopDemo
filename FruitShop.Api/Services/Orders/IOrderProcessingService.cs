using FruitShop.Api.Models;

namespace FruitShop.Api.Services;

public interface IOrderProcessingService
{
    Task<Order> CreateAndSubmitOrderAsync(
        string customerTier,
        IReadOnlyCollection<OrderItemSubmission> items,
        CancellationToken cancellationToken = default);
}