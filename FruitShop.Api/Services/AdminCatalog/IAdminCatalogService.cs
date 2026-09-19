using FruitShop.Api.Models;

namespace FruitShop.Api.Services;

public interface IAdminCatalogService
{
    Task<AdminCatalogResponse> GetCatalogAsync(AdminCatalogPageRequest pageRequest, CancellationToken cancellationToken = default);
    Task<AdminProductResponse> CreateProductAsync(CreateAdminProductRequest request, CancellationToken cancellationToken = default);
    Task<AdminCatalogOperationResult<AdminProductVariantResponse>> CreateVariantAsync(
        long productId,
        CreateProductVariantRequest request,
        CancellationToken cancellationToken = default);
    Task<AdminCatalogOperationResult<AdminProductVariantResponse>> SetVariantStatusAsync(
        long variantId,
        SetProductVariantStatusRequest request,
        CancellationToken cancellationToken = default);
    Task<AdminCatalogOperationResult<AdminPriceRuleResponse>> CreatePriceRuleAsync(
        CreatePriceRuleRequest request,
        CancellationToken cancellationToken = default);
    Task<AdminCatalogOperationResult<ProductVariantPriceRulesResponse>> GetProductVariantPriceRulesAsync(
        long variantId,
        CancellationToken cancellationToken = default);
    Task<AdminCatalogOperationResult<AdminPriceRuleResponse>> AssignPriceRuleToProductVariantAsync(
        long variantId,
        long priceRuleId,
        CancellationToken cancellationToken = default);
    Task<AdminCatalogOperationResult> RemovePriceRuleFromProductVariantAsync(
        long variantId,
        long priceRuleId,
        CancellationToken cancellationToken = default);
}