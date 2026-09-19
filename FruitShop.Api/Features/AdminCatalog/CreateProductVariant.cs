using FruitShop.Api.Models;
using FruitShop.Api.Services;
using MediatR;

namespace FruitShop.Api.Features.AdminCatalog;

public sealed record CreateProductVariantCommand(long ProductId, CreateProductVariantRequest Request)
    : IRequest<AdminCatalogOperationResult<AdminProductVariantResponse>>;

public sealed class CreateProductVariantCommandHandler(IAdminCatalogService catalogService)
    : IRequestHandler<CreateProductVariantCommand, AdminCatalogOperationResult<AdminProductVariantResponse>>
{
    public Task<AdminCatalogOperationResult<AdminProductVariantResponse>> Handle(
        CreateProductVariantCommand request,
        CancellationToken cancellationToken) =>
        catalogService.CreateVariantAsync(request.ProductId, request.Request, cancellationToken);
}