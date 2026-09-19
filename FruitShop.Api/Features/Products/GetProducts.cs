using AutoMapper;
using FruitShop.Api.Features.Orders;
using FruitShop.Api.Models;
using FruitShop.Api.Repositories;
using MediatR;

namespace FruitShop.Api.Features.Products;

public sealed record GetProductsQuery(PageRequest PageRequest) : IRequest<PagedResult<ProductResponse>>;

public sealed class GetProductsQueryHandler(IProductRepository repository, IMapper mapper)
    : IRequestHandler<GetProductsQuery, PagedResult<ProductResponse>>
{
    public async Task<PagedResult<ProductResponse>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var products = await repository.GetProductsAsync(request.PageRequest, cancellationToken);
        return new PagedResult<ProductResponse>(
            mapper.Map<List<ProductResponse>>(products.Items),
            products.Page,
            products.PageSize,
            products.TotalCount);
    }
}

public sealed record ProductResponse(long Id, string Name, IReadOnlyList<ProductVariantResponse> Variants);

public sealed record ProductVariantResponse(
    long Id,
    string Sku,
    string UomCode,
    decimal UomFactor,
    decimal BasePrice,
    bool IsActive,
    UnitOfMeasureResponse UnitOfMeasure,
    IReadOnlyList<RelatedPriceRuleResponse> ActivePriceRules);