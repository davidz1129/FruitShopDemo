using FruitShop.Api.Data;
using FruitShop.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FruitShop.Api.Repositories;

public class PriceRuleRepository(FruitShopDbContext dbContext) : IPriceRuleRepository
{
    public async Task<List<PriceRule>> GetActiveRulesAsync(long variantId, string customerTier, CancellationToken cancellationToken = default)
    {
        string normalizedCustomerTier = customerTier.Trim().ToUpperInvariant();

        return await dbContext.PriceRules
            .AsNoTracking()
            .Include(rule => rule.Conditions)
            .Include(rule => rule.Actions)
            .Where(rule => rule.Status == RuleStatus.Active)
            .Where(rule => rule.AppliesToAllVariants || rule.TargetVariants.Any(target => target.VariantId == variantId))
            .Where(rule => !rule.TargetCustomerTiers.Any() || rule.TargetCustomerTiers.Any(target => target.CustomerTier.ToUpper() == normalizedCustomerTier))
            .OrderByDescending(rule => rule.Priority)
            .ThenBy(rule => rule.Name)
            .ToListAsync(cancellationToken);
    }
}