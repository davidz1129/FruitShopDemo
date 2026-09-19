using FruitShop.Api.Models;
using FruitShop.Api.Services;
using MediatR;

namespace FruitShop.Api.Features.AdminCatalog;

public sealed record GetProductVariantPriceRulesQuery(long VariantId)
    : IRequest<AdminCatalogOperationResult<ProductVariantPriceRulesResponse>>;

public sealed class GetProductVariantPriceRulesQueryHandler(IAdminCatalogService catalogService)
    : IRequestHandler<GetProductVariantPriceRulesQuery, AdminCatalogOperationResult<ProductVariantPriceRulesResponse>>
{
    public Task<AdminCatalogOperationResult<ProductVariantPriceRulesResponse>> Handle(
        GetProductVariantPriceRulesQuery request,
        CancellationToken cancellationToken) =>
        catalogService.GetProductVariantPriceRulesAsync(request.VariantId, cancellationToken);
}

public sealed record AssignPriceRuleToProductVariantCommand(long VariantId, long PriceRuleId)
    : IRequest<AdminCatalogOperationResult<AdminPriceRuleResponse>>;

public sealed class AssignPriceRuleToProductVariantCommandHandler(IAdminCatalogService catalogService)
    : IRequestHandler<AssignPriceRuleToProductVariantCommand, AdminCatalogOperationResult<AdminPriceRuleResponse>>
{
    public Task<AdminCatalogOperationResult<AdminPriceRuleResponse>> Handle(
        AssignPriceRuleToProductVariantCommand request,
        CancellationToken cancellationToken) =>
        catalogService.AssignPriceRuleToProductVariantAsync(request.VariantId, request.PriceRuleId, cancellationToken);
}

public sealed record RemovePriceRuleFromProductVariantCommand(long VariantId, long PriceRuleId)
    : IRequest<AdminCatalogOperationResult>;

public sealed class RemovePriceRuleFromProductVariantCommandHandler(IAdminCatalogService catalogService)
    : IRequestHandler<RemovePriceRuleFromProductVariantCommand, AdminCatalogOperationResult>
{
    public Task<AdminCatalogOperationResult> Handle(
        RemovePriceRuleFromProductVariantCommand request,
        CancellationToken cancellationToken) =>
        catalogService.RemovePriceRuleFromProductVariantAsync(request.VariantId, request.PriceRuleId, cancellationToken);
}