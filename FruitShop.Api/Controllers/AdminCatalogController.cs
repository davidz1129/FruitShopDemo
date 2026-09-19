using FruitShop.Api.Features.AdminCatalog;
using FruitShop.Api.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FruitShop.Api.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminCatalogController(ISender sender) : ControllerBase
{
    [HttpGet("catalog")]
    [ProducesResponseType<AdminCatalogResponse>(StatusCodes.Status200OK)]
    public Task<AdminCatalogResponse> GetCatalog(
        [FromQuery] AdminCatalogPageRequest pageRequest,
        CancellationToken cancellationToken) =>
        sender.Send(new GetAdminCatalogQuery(pageRequest), cancellationToken);

    [HttpPost("products")]
    [ProducesResponseType<AdminProductResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AdminProductResponse>> CreateProduct(
        CreateAdminProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = await sender.Send(new CreateAdminProductCommand(request), cancellationToken);
        return Created($"/api/admin/products/{product.Id}", product);
    }

    [HttpPost("products/{productId:long}/variants")]
    [ProducesResponseType<AdminProductVariantResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AdminProductVariantResponse>> CreateVariant(
        long productId,
        CreateProductVariantRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateProductVariantCommand(productId, request), cancellationToken);
        return result.Status switch
        {
            AdminCatalogOperationStatus.ProductNotFound => NotFound(),
            AdminCatalogOperationStatus.UnitOfMeasureNotFound or AdminCatalogOperationStatus.DuplicateSku => ValidationError(result.Message!),
            _ => Created($"/api/admin/variants/{result.Value!.Id}", result.Value)
        };
    }

    [HttpPatch("variants/{variantId:long}/status")]
    [ProducesResponseType<AdminProductVariantResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminProductVariantResponse>> SetVariantStatus(
        long variantId,
        SetProductVariantStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SetProductVariantStatusCommand(variantId, request), cancellationToken);
        return result.Status == AdminCatalogOperationStatus.VariantNotFound ? NotFound() : Ok(result.Value);
    }

    [HttpGet("variants/{variantId:long}/price-rules")]
    [ProducesResponseType<ProductVariantPriceRulesResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductVariantPriceRulesResponse>> GetProductVariantPriceRules(
        long variantId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetProductVariantPriceRulesQuery(variantId), cancellationToken);
        return result.Status == AdminCatalogOperationStatus.VariantNotFound ? NotFound() : Ok(result.Value);
    }

    [HttpPut("variants/{variantId:long}/price-rules/{priceRuleId:long}")]
    [ProducesResponseType<AdminPriceRuleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AdminPriceRuleResponse>> AssignPriceRuleToProductVariant(
        long variantId,
        long priceRuleId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new AssignPriceRuleToProductVariantCommand(variantId, priceRuleId), cancellationToken);
        return result.Status switch
        {
            AdminCatalogOperationStatus.VariantNotFound or AdminCatalogOperationStatus.PriceRuleNotFound => NotFound(),
            AdminCatalogOperationStatus.VariantInactive or AdminCatalogOperationStatus.PriceRuleAppliesToAllVariants => ValidationError(result.Message!),
            _ => Ok(result.Value)
        };
    }

    [HttpDelete("variants/{variantId:long}/price-rules/{priceRuleId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RemovePriceRuleFromProductVariant(
        long variantId,
        long priceRuleId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RemovePriceRuleFromProductVariantCommand(variantId, priceRuleId), cancellationToken);
        return result.Status switch
        {
            AdminCatalogOperationStatus.VariantNotFound or AdminCatalogOperationStatus.PriceRuleNotFound or AdminCatalogOperationStatus.PriceRuleNotAssigned => NotFound(),
            AdminCatalogOperationStatus.PriceRuleAppliesToAllVariants => ValidationError(result.Message!),
            _ => NoContent()
        };
    }

    [HttpPost("price-rules")]
    [ProducesResponseType<AdminPriceRuleResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AdminPriceRuleResponse>> CreatePriceRule(
        CreatePriceRuleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreatePriceRuleCommand(request), cancellationToken);
        return result.Status == AdminCatalogOperationStatus.InvalidRule
            ? ValidationError(result.Message!)
            : Created($"/api/admin/price-rules/{result.Value!.Id}", result.Value);
    }

    private ActionResult ValidationError(string message)
    {
        ModelState.AddModelError("request", message);
        return ValidationProblem(ModelState);
    }
}