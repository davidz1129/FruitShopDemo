using FruitShop.Api.Models;

namespace FruitShop.Api.Repositories;

public interface IProductRepository
{
    Task<PagedResult<Product>> GetProductsAsync(PageRequest pageRequest, CancellationToken cancellationToken = default);
    Task<ProductVariant?> GetVariantByIdAsync(long id, CancellationToken cancellationToken = default);
}