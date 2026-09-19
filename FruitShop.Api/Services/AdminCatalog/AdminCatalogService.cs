using AutoMapper;
using FruitShop.Api.Data;
using FruitShop.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FruitShop.Api.Services;

public sealed class AdminCatalogService(FruitShopDbContext dbContext, IMapper mapper) : IAdminCatalogService
{
    public async Task<AdminCatalogResponse> GetCatalogAsync(
        AdminCatalogPageRequest pageRequest,
        CancellationToken cancellationToken = default)
    {
        var productsPageRequest = pageRequest.ProductsPageRequest;
        var priceRulesPageRequest = pageRequest.PriceRulesPageRequest;
        var productsQuery = dbContext.Products
            .AsNoTracking()
            .OrderBy(product => product.Name)
            .ThenBy(product => product.Id);
        var productTotalCount = await productsQuery.CountAsync(cancellationToken);
        var products = await productsQuery
            .Include(product => product.Variants)
                .ThenInclude(variant => variant.UnitOfMeasure)
            .Skip(productsPageRequest.Skip)
            .Take(productsPageRequest.PageSize)
            .ToListAsync(cancellationToken);
        var unitsOfMeasure = await dbContext.UnitsOfMeasure
            .AsNoTracking()
            .OrderBy(unit => unit.Code)
            .ToListAsync(cancellationToken);
        var priceRulesQuery = dbContext.PriceRules
            .AsNoTracking()
            .OrderByDescending(rule => rule.Priority)
            .ThenBy(rule => rule.Name);
        var priceRuleTotalCount = await priceRulesQuery.CountAsync(cancellationToken);
        var priceRules = await priceRulesQuery
            .Include(rule => rule.Conditions)
            .Include(rule => rule.Actions)
            .Include(rule => rule.TargetVariants)
            .Include(rule => rule.TargetCustomerTiers)
            .AsSplitQuery()
            .Skip(priceRulesPageRequest.Skip)
            .Take(priceRulesPageRequest.PageSize)
            .ToListAsync(cancellationToken);

        return new AdminCatalogResponse(
            new PagedResult<AdminProductResponse>(
                mapper.Map<List<AdminProductResponse>>(products),
                productsPageRequest.Page,
                productsPageRequest.PageSize,
                productTotalCount),
            mapper.Map<List<UnitOfMeasureResponse>>(unitsOfMeasure),
            new PagedResult<AdminPriceRuleResponse>(
                mapper.Map<List<AdminPriceRuleResponse>>(priceRules),
                priceRulesPageRequest.Page,
                priceRulesPageRequest.PageSize,
                priceRuleTotalCount));
    }

    public async Task<AdminProductResponse> CreateProductAsync(
        CreateAdminProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var product = mapper.Map<Product>(request);
        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync(cancellationToken);
        return mapper.Map<AdminProductResponse>(product);
    }

    public async Task<AdminCatalogOperationResult<AdminProductVariantResponse>> CreateVariantAsync(
        long productId,
        CreateProductVariantRequest request,
        CancellationToken cancellationToken = default)
    {
        var product = await dbContext.Products.SingleOrDefaultAsync(product => product.Id == productId, cancellationToken);
        if (product is null)
        {
            return new(AdminCatalogOperationStatus.ProductNotFound, null);
        }

        var uomCode = request.UomCode.Trim();
        var unitOfMeasure = await dbContext.UnitsOfMeasure.SingleOrDefaultAsync(unit => unit.Code == uomCode, cancellationToken);
        if (unitOfMeasure is null)
        {
            return new(AdminCatalogOperationStatus.UnitOfMeasureNotFound, null, "Choose a valid unit of measure.");
        }

        var sku = request.Sku.Trim();
        if (await dbContext.ProductVariants.AnyAsync(variant => variant.Sku == sku, cancellationToken))
        {
            return new(AdminCatalogOperationStatus.DuplicateSku, null, "SKU values must be unique.");
        }

        var variant = mapper.Map<ProductVariant>(request);
        variant.ProductId = product.Id;
        variant.UnitOfMeasure = unitOfMeasure;
        dbContext.ProductVariants.Add(variant);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new(AdminCatalogOperationStatus.Success, mapper.Map<AdminProductVariantResponse>(variant));
    }

    public async Task<AdminCatalogOperationResult<AdminProductVariantResponse>> SetVariantStatusAsync(
        long variantId,
        SetProductVariantStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var variant = await dbContext.ProductVariants
            .Include(candidate => candidate.UnitOfMeasure)
            .SingleOrDefaultAsync(candidate => candidate.Id == variantId, cancellationToken);
        if (variant is null)
        {
            return new(AdminCatalogOperationStatus.VariantNotFound, null);
        }

        variant.IsActive = request.IsActive!.Value;
        await dbContext.SaveChangesAsync(cancellationToken);
        return new(AdminCatalogOperationStatus.Success, mapper.Map<AdminProductVariantResponse>(variant));
    }

    public async Task<AdminCatalogOperationResult<AdminPriceRuleResponse>> CreatePriceRuleAsync(
        CreatePriceRuleRequest request,
        CancellationToken cancellationToken = default)
    {
        var targetVariantIds = request.VariantIds.Distinct().ToList();
        if (!request.AppliesToAllVariants && targetVariantIds.Count == 0)
        {
            return new(AdminCatalogOperationStatus.InvalidRule, null, "Select at least one product variant or apply the rule to all variants.");
        }

        if (targetVariantIds.Count > 0)
        {
            var activeVariantCount = await dbContext.ProductVariants
                .CountAsync(variant => variant.IsActive && targetVariantIds.Contains(variant.Id), cancellationToken);
            if (activeVariantCount != targetVariantIds.Count)
            {
                return new(AdminCatalogOperationStatus.InvalidRule, null, "Price rules can only target active product variants.");
            }
        }

        var priceRule = mapper.Map<PriceRule>(request);
        dbContext.PriceRules.Add(priceRule);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new(AdminCatalogOperationStatus.Success, mapper.Map<AdminPriceRuleResponse>(priceRule));
    }

    public async Task<AdminCatalogOperationResult<ProductVariantPriceRulesResponse>> GetProductVariantPriceRulesAsync(
        long variantId,
        CancellationToken cancellationToken = default)
    {
        var variant = await dbContext.ProductVariants
            .AsNoTracking()
            .Include(candidate => candidate.UnitOfMeasure)
            .SingleOrDefaultAsync(candidate => candidate.Id == variantId, cancellationToken);
        if (variant is null)
        {
            return new(AdminCatalogOperationStatus.VariantNotFound, null);
        }

        var assignedRules = await PriceRulesWithDetails()
            .AsNoTracking()
            .Where(rule => !rule.AppliesToAllVariants && rule.TargetVariants.Any(target => target.VariantId == variantId))
            .OrderByDescending(rule => rule.Priority)
            .ThenBy(rule => rule.Name)
            .ToListAsync(cancellationToken);
        var availableRules = await PriceRulesWithDetails()
            .AsNoTracking()
            .Where(rule => !rule.AppliesToAllVariants && !rule.TargetVariants.Any(target => target.VariantId == variantId))
            .OrderByDescending(rule => rule.Priority)
            .ThenBy(rule => rule.Name)
            .ToListAsync(cancellationToken);
        var globalRules = await PriceRulesWithDetails()
            .AsNoTracking()
            .Where(rule => rule.AppliesToAllVariants)
            .OrderByDescending(rule => rule.Priority)
            .ThenBy(rule => rule.Name)
            .ToListAsync(cancellationToken);

        return new(
            AdminCatalogOperationStatus.Success,
            new ProductVariantPriceRulesResponse(
                mapper.Map<AdminProductVariantResponse>(variant),
                mapper.Map<List<AdminPriceRuleResponse>>(assignedRules),
                mapper.Map<List<AdminPriceRuleResponse>>(availableRules),
                mapper.Map<List<AdminPriceRuleResponse>>(globalRules)));
    }

    public async Task<AdminCatalogOperationResult<AdminPriceRuleResponse>> AssignPriceRuleToProductVariantAsync(
        long variantId,
        long priceRuleId,
        CancellationToken cancellationToken = default)
    {
        var variant = await dbContext.ProductVariants
            .SingleOrDefaultAsync(candidate => candidate.Id == variantId, cancellationToken);
        if (variant is null)
        {
            return new(AdminCatalogOperationStatus.VariantNotFound, null);
        }

        if (!variant.IsActive)
        {
            return new(AdminCatalogOperationStatus.VariantInactive, null, "Price rules can only target active product variants.");
        }

        var priceRule = await PriceRulesWithDetails()
            .SingleOrDefaultAsync(candidate => candidate.Id == priceRuleId, cancellationToken);
        if (priceRule is null)
        {
            return new(AdminCatalogOperationStatus.PriceRuleNotFound, null);
        }

        if (priceRule.AppliesToAllVariants)
        {
            return new(AdminCatalogOperationStatus.PriceRuleAppliesToAllVariants, null, "Rules that apply to all variants cannot be targeted individually.");
        }

        if (priceRule.TargetVariants.All(target => target.VariantId != variantId))
        {
            priceRule.TargetVariants.Add(new RuleTargetVariant { RuleId = priceRule.Id, VariantId = variantId });
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return new(AdminCatalogOperationStatus.Success, mapper.Map<AdminPriceRuleResponse>(priceRule));
    }

    public async Task<AdminCatalogOperationResult> RemovePriceRuleFromProductVariantAsync(
        long variantId,
        long priceRuleId,
        CancellationToken cancellationToken = default)
    {
        var variantExists = await dbContext.ProductVariants
            .AnyAsync(candidate => candidate.Id == variantId, cancellationToken);
        if (!variantExists)
        {
            return new(AdminCatalogOperationStatus.VariantNotFound);
        }

        var priceRule = await dbContext.PriceRules
            .Include(candidate => candidate.TargetVariants)
            .SingleOrDefaultAsync(candidate => candidate.Id == priceRuleId, cancellationToken);
        if (priceRule is null)
        {
            return new(AdminCatalogOperationStatus.PriceRuleNotFound);
        }

        if (priceRule.AppliesToAllVariants)
        {
            return new(AdminCatalogOperationStatus.PriceRuleAppliesToAllVariants, "Rules that apply to all variants cannot be targeted individually.");
        }

        var target = priceRule.TargetVariants.SingleOrDefault(candidate => candidate.VariantId == variantId);
        if (target is null)
        {
            return new(AdminCatalogOperationStatus.PriceRuleNotAssigned);
        }

        dbContext.RuleTargetVariants.Remove(target);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new(AdminCatalogOperationStatus.Success);
    }

    private IQueryable<PriceRule> PriceRulesWithDetails() => dbContext.PriceRules
        .Include(rule => rule.Conditions)
        .Include(rule => rule.Actions)
        .Include(rule => rule.TargetVariants)
        .Include(rule => rule.TargetCustomerTiers)
        .AsSplitQuery();
}