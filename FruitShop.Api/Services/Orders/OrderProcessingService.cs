using FruitShop.Api.Models;
using FruitShop.Api.Data;
using FruitShop.Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FruitShop.Api.Services;

public class OrderProcessingService : IOrderProcessingService
{
    private readonly FruitShopDbContext _dbContext;
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IOrderItemCalculationService _orderItemCalculationService;

    public OrderProcessingService(
        FruitShopDbContext dbContext,
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IOrderItemCalculationService orderItemCalculationService)
    {
        _dbContext = dbContext;
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _orderItemCalculationService = orderItemCalculationService;
    }

    public async Task<Order> CreateAndSubmitOrderAsync(
        string customerTier,
        IReadOnlyCollection<OrderItemSubmission> items,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(customerTier);
        ArgumentNullException.ThrowIfNull(items);

        if (items.Count == 0)
        {
            throw new ArgumentException("An order must contain at least one item.", nameof(items));
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var order = await _orderRepository.AddAsync(
                new Order { CustomerTier = customerTier },
                cancellationToken);

            foreach (var item in items)
            {
                await AddItemToOrderAsync(order.Id, item.VariantId, item.Quantity, cancellationToken);
            }

            var submittedOrder = await SubmitOrderAsync(order.Id, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return submittedOrder;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private async Task<Order> AddItemToOrderAsync(long orderId, long variantId, decimal quantity, CancellationToken cancellationToken = default)
    {
        // 1. Load the Aggregate Root and target Variant
        Order order = await _orderRepository.GetByIdAsync(orderId, cancellationToken) 
            ?? throw new NotFoundException(nameof(Order), orderId);
            
        ProductVariant variant = await _productRepository.GetVariantByIdAsync(variantId, cancellationToken)
            ?? throw new NotFoundException(nameof(ProductVariant), variantId);

        decimal projectedCartSubtotal = order.Items
            .Where(item => item.VariantId != variantId)
            .Sum(item => item.TotalLineAmount) + (variant.BasePrice * quantity);

        // 2. Resolve rules and calculate the price using the order-specific context.
        var quote = await _orderItemCalculationService.QuoteAsync(
            new OrderItemQuoteRequest(
                variant,
                order.CustomerTier,
                quantity,
                projectedCartSubtotal,
                order.CreatedAt),
            cancellationToken);

        // 3. Delegate state mutation to the Aggregate Root
        // The OrderItem entity will automatically calculate TotalLineAmount = (Quantity * finalUnitPrice)
        order.AddItem(
            variant,
            quantity,
            quote.PriceCalculation.UnitPriceApplied,
            quote.PriceCalculation.PriceChangeReason);

        return order;
    }

    private async Task<Order> SubmitOrderAsync(long orderId, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), orderId);

        order.Submit();
        await _orderRepository.SaveChangesAsync(cancellationToken);

        return order;
    }
}

public sealed record OrderItemSubmission(long VariantId, decimal Quantity);