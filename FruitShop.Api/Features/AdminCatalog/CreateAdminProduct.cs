using FruitShop.Api.Models;
using FruitShop.Api.Services;
using MediatR;

namespace FruitShop.Api.Features.AdminCatalog;

public sealed record CreateAdminProductCommand(CreateAdminProductRequest Request) : IRequest<AdminProductResponse>;

public sealed class CreateAdminProductCommandHandler(IAdminCatalogService catalogService)
    : IRequestHandler<CreateAdminProductCommand, AdminProductResponse>
{
    public Task<AdminProductResponse> Handle(CreateAdminProductCommand request, CancellationToken cancellationToken) =>
        catalogService.CreateProductAsync(request.Request, cancellationToken);
}