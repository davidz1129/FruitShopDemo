using FruitShop.Api.Models;

namespace FruitShop.Api.Repositories;

public interface IPriceRuleRepository
{
    Task<List<PriceRule>> GetActiveRulesAsync(long variantId, string customerTier, CancellationToken cancellationToken = default);
}