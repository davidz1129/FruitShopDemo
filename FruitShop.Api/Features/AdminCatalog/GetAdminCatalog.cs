using FruitShop.Api.Models;
using FruitShop.Api.Services;
using MediatR;

namespace FruitShop.Api.Features.AdminCatalog;

public sealed record GetAdminCatalogQuery(AdminCatalogPageRequest PageRequest) : IRequest<AdminCatalogResponse>;

public sealed class GetAdminCatalogQueryHandler(IAdminCatalogService catalogService)
    : IRequestHandler<GetAdminCatalogQuery, AdminCatalogResponse>
{
    public Task<AdminCatalogResponse> Handle(GetAdminCatalogQuery request, CancellationToken cancellationToken) =>
        catalogService.GetCatalogAsync(request.PageRequest, cancellationToken);
}