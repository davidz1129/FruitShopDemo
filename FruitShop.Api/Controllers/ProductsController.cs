using FruitShop.Api.Features.Products;
using FruitShop.Api.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FruitShop.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<ProductResponse>>(StatusCodes.Status200OK)]
    public Task<PagedResult<ProductResponse>> GetAll([FromQuery] PageRequest pageRequest, CancellationToken cancellationToken) =>
        sender.Send(new GetProductsQuery(pageRequest), cancellationToken);
}