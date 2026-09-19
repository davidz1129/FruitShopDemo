using FruitShop.Api.Data;
using FruitShop.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FruitShop.Api.Repositories;

public class ProductRepository(FruitShopDbContext dbContext) : IProductRepository
{
    public async Task<PagedResult<Product>> GetProductsAsync(PageRequest pageRequest, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Products
            .AsNoTracking()
            .Where(product => product.Variants.Any(variant => variant.IsActive));
        var totalCount = await query.CountAsync(cancellationToken);
        var products = await query
            .Include(product => product.Variants)
                .ThenInclude(variant => variant.UnitOfMeasure)
            .OrderBy(product => product.Name)
            .ThenBy(product => product.Id)
            .Skip(pageRequest.Skip)
            .Take(pageRequest.PageSize)
            .Select(product => new Product
            {
                Id = product.Id,
                Name = product.Name,
                Variants = product.Variants
                    .Where(variant => variant.IsActive)
                    .OrderBy(variant => variant.Sku)
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<Product>(products, pageRequest.Page, pageRequest.PageSize, totalCount);
    }

    public async Task<ProductVariant?> GetVariantByIdAsync(long id, CancellationToken cancellationToken = default) =>
        await dbContext.ProductVariants
            .Include(variant => variant.UnitOfMeasure)
            .SingleOrDefaultAsync(variant => variant.Id == id && variant.IsActive, cancellationToken);
}