using FruitShop.Api.Data;
using FruitShop.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FruitShop.Api.Repositories;

public class OrderRepository(FruitShopDbContext dbContext) : IOrderRepository
{
    public async Task<Order> AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync(cancellationToken);
        return order;
    }

    public async Task<PagedResult<Order>> GetAllAsync(PageRequest pageRequest, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Orders
            .AsNoTracking()
            .OrderByDescending(order => order.CreatedAt)
            .ThenByDescending(order => order.Id);
        var totalCount = await query.CountAsync(cancellationToken);
        var orders = await query
            .Skip(pageRequest.Skip)
            .Take(pageRequest.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Order>(orders, pageRequest.Page, pageRequest.PageSize, totalCount);
    }

    public async Task<Order?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        await dbContext.Orders
            .Include(order => order.Items)
                .ThenInclude(item => item.Variant)
                    .ThenInclude(variant => variant.UnitOfMeasure)
            .SingleOrDefaultAsync(order => order.Id == id, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.SaveChangesAsync(cancellationToken);
}