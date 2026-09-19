using FruitShop.Api.Models;
using FruitShop.Api.Services;
using MediatR;

namespace FruitShop.Api.Features.AdminCatalog;

public sealed record CreatePriceRuleCommand(CreatePriceRuleRequest Request)
    : IRequest<AdminCatalogOperationResult<AdminPriceRuleResponse>>;

public sealed class CreatePriceRuleCommandHandler(IAdminCatalogService catalogService)
    : IRequestHandler<CreatePriceRuleCommand, AdminCatalogOperationResult<AdminPriceRuleResponse>>
{
    public Task<AdminCatalogOperationResult<AdminPriceRuleResponse>> Handle(
        CreatePriceRuleCommand request,
        CancellationToken cancellationToken) =>
        catalogService.CreatePriceRuleAsync(request.Request, cancellationToken);
}