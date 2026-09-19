using FruitShop.Api.Models;
using FruitShop.Api.Services;
using MediatR;

namespace FruitShop.Api.Features.AdminCatalog;

public sealed record SetProductVariantStatusCommand(long VariantId, SetProductVariantStatusRequest Request)
    : IRequest<AdminCatalogOperationResult<AdminProductVariantResponse>>;

public sealed class SetProductVariantStatusCommandHandler(IAdminCatalogService catalogService)
    : IRequestHandler<SetProductVariantStatusCommand, AdminCatalogOperationResult<AdminProductVariantResponse>>
{
    public Task<AdminCatalogOperationResult<AdminProductVariantResponse>> Handle(
        SetProductVariantStatusCommand request,
        CancellationToken cancellationToken) =>
        catalogService.SetVariantStatusAsync(request.VariantId, request.Request, cancellationToken);
}