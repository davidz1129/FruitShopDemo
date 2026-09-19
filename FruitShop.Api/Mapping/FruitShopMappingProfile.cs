using AutoMapper;
using FruitShop.Api.Controllers;
using FruitShop.Api.Features.Orders;
using FruitShop.Api.Features.Products;
using FruitShop.Api.Models;
using FruitShop.Api.Services;

namespace FruitShop.Api.Mapping;

public sealed class FruitShopMappingProfile : Profile
{
    public FruitShopMappingProfile()
    {
        CreateMap<DateTime, DateTimeOffset>()
            .ConvertUsing(value => new DateTimeOffset(DateTime.SpecifyKind(
                value,
                value.Kind == DateTimeKind.Unspecified ? DateTimeKind.Utc : value.Kind)));

        CreateMap<SubmitOrderRequest, SubmitOrderCommand>()
            .ForCtorParam(nameof(SubmitOrderCommand.CustomerTier), options => options.MapFrom(source => NormalizeCustomerTier(source.CustomerTier)));
        CreateMap<CalculateOrderItemRequest, CalculateOrderItemQuery>()
            .ForCtorParam(nameof(CalculateOrderItemQuery.CustomerTier), options => options.MapFrom(source => NormalizeCustomerTier(source.CustomerTier)));
        CreateMap<SubmitOrderItemRequest, OrderItemSubmission>();
        CreateMap<Product, ProductResponse>()
            .ForCtorParam(nameof(ProductResponse.Variants), options => options.MapFrom(source => source.Variants.OrderBy(variant => variant.Sku)));
        CreateMap<ProductVariant, ProductVariantResponse>()
            .ForCtorParam(nameof(ProductVariantResponse.UnitOfMeasure), options => options.MapFrom(source => source.UnitOfMeasure))
            .ForCtorParam(nameof(ProductVariantResponse.ActivePriceRules), options => options.MapFrom(source => source.TargetRules
                .Select(target => target.Rule)
                .OrderByDescending(rule => rule.Priority)
                .ThenBy(rule => rule.Name)));

        CreateMap<Order, OrderListItemResponse>();
        CreateMap<Order, OrderResponse>()
            .ForCtorParam(nameof(OrderResponse.Items), options => options.MapFrom(source => source.Items.OrderBy(item => item.Id)));
        CreateMap<OrderItem, OrderItemResponse>()
            .ForCtorParam(nameof(OrderItemResponse.Sku), options => options.MapFrom(source => source.Variant.Sku))
            .ForCtorParam(nameof(OrderItemResponse.UnitOfMeasure), options => options.MapFrom(source => source.Variant.UomCode));
        CreateMap<OrderItemPreview, CalculateOrderItemResponse>()
            .ForCtorParam(nameof(CalculateOrderItemResponse.VariantId), options => options.MapFrom(source => source.Variant.Id))
            .ForCtorParam(nameof(CalculateOrderItemResponse.Sku), options => options.MapFrom(source => source.Variant.Sku))
            .ForCtorParam(nameof(CalculateOrderItemResponse.UnitOfMeasure), options => options.MapFrom(source => source.Variant.UomCode))
            .ForCtorParam(nameof(CalculateOrderItemResponse.BaseUnitPrice), options => options.MapFrom(source => source.Variant.BasePrice))
            .ForCtorParam(nameof(CalculateOrderItemResponse.UnitPriceApplied), options => options.MapFrom(source => source.PriceCalculation.UnitPriceApplied))
            .ForCtorParam(nameof(CalculateOrderItemResponse.LineSubtotal), options => options.MapFrom(source => Math.Round(
                source.Quantity * source.PriceCalculation.UnitPriceApplied,
                2,
                MidpointRounding.AwayFromZero)))
            .ForCtorParam(nameof(CalculateOrderItemResponse.PriceChangeReason), options => options.MapFrom(source => source.PriceCalculation.PriceChangeReason))
            .ForCtorParam(nameof(CalculateOrderItemResponse.ActivePriceRules), options => options.MapFrom(source => source.ActiveRules));
        CreateMap<PriceRule, RelatedPriceRuleResponse>();
        CreateMap<PriceRuleCondition, PriceRuleConditionResponse>();
        CreateMap<PriceRuleAction, PriceRuleActionResponse>()
            .ForCtorParam(nameof(PriceRuleActionResponse.ActionType), options => options.MapFrom(source => source.ActionType.ToString()))
            .ForCtorParam(nameof(PriceRuleActionResponse.CalculationBase), options => options.MapFrom(source => source.CalculationBase.ToString()));

        CreateMap<Product, AdminProductResponse>()
            .ForCtorParam(nameof(AdminProductResponse.Variants), options => options.MapFrom(source => source.Variants.OrderBy(variant => variant.Sku)));
        CreateMap<ProductVariant, AdminProductVariantResponse>()
            .ForCtorParam(nameof(AdminProductVariantResponse.MeasureType), options => options.MapFrom(source => source.UnitOfMeasure.MeasureType));
        CreateMap<UnitOfMeasure, UnitOfMeasureResponse>();
        CreateMap<PriceRule, AdminPriceRuleResponse>()
            .ForCtorParam(nameof(AdminPriceRuleResponse.TargetVariantIds), options => options.MapFrom(source => source.TargetVariants
                .OrderBy(target => target.VariantId)
                .Select(target => target.VariantId)))
            .ForCtorParam(nameof(AdminPriceRuleResponse.TargetCustomerTiers), options => options.MapFrom(source => source.TargetCustomerTiers
                .OrderBy(target => target.CustomerTier)
                .Select(target => target.CustomerTier)));
        CreateMap<PriceRuleCondition, AdminPriceRuleConditionResponse>();
        CreateMap<PriceRuleAction, AdminPriceRuleActionResponse>();

        CreateMap<CreateAdminProductRequest, Product>()
            .ForMember(destination => destination.Id, options => options.Ignore())
            .ForMember(destination => destination.Variants, options => options.Ignore())
            .ForMember(destination => destination.Name, options => options.MapFrom(source => source.Name.Trim()));
        CreateMap<CreateProductVariantRequest, ProductVariant>()
            .ForMember(destination => destination.Id, options => options.Ignore())
            .ForMember(destination => destination.ProductId, options => options.Ignore())
            .ForMember(destination => destination.Product, options => options.Ignore())
            .ForMember(destination => destination.UnitOfMeasure, options => options.Ignore())
            .ForMember(destination => destination.TargetRules, options => options.Ignore())
            .ForMember(destination => destination.Sku, options => options.MapFrom(source => source.Sku.Trim()))
            .ForMember(destination => destination.UomCode, options => options.MapFrom(source => source.UomCode.Trim()))
            .ForMember(destination => destination.IsActive, options => options.MapFrom(_ => true));
        CreateMap<CreatePriceRuleRequest, PriceRule>()
            .ForMember(destination => destination.Id, options => options.Ignore())
            .ForMember(destination => destination.Name, options => options.MapFrom(source => source.Name.Trim()))
            .ForMember(destination => destination.Status, options => options.MapFrom(_ => RuleStatus.Active))
            .ForMember(destination => destination.TargetVariants, options => options.MapFrom(source => source.AppliesToAllVariants
                ? Enumerable.Empty<long>()
                : source.VariantIds.Distinct()))
            .ForMember(destination => destination.TargetCustomerTiers, options => options.MapFrom(source => source.CustomerTiers
                .Select(NormalizeCustomerTier)
                .Where(customerTier => !string.IsNullOrWhiteSpace(customerTier))
                .Distinct()));
        CreateMap<CreatePriceRuleConditionRequest, PriceRuleCondition>()
            .ForMember(destination => destination.Id, options => options.Ignore())
            .ForMember(destination => destination.RuleId, options => options.Ignore())
            .ForMember(destination => destination.Rule, options => options.Ignore())
            .ForMember(destination => destination.Attribute, options => options.MapFrom(source => source.Attribute.Trim()))
            .ForMember(destination => destination.Operator, options => options.MapFrom(source => source.Operator.Trim()))
            .ForMember(destination => destination.Value, options => options.MapFrom(source => source.Value.Trim()));
        CreateMap<CreatePriceRuleActionRequest, PriceRuleAction>()
            .ForMember(destination => destination.Id, options => options.Ignore())
            .ForMember(destination => destination.RuleId, options => options.Ignore())
            .ForMember(destination => destination.Rule, options => options.Ignore());
        CreateMap<long, RuleTargetVariant>()
            .ForMember(destination => destination.RuleId, options => options.Ignore())
            .ForMember(destination => destination.Rule, options => options.Ignore())
            .ForMember(destination => destination.Variant, options => options.Ignore())
            .ForMember(destination => destination.VariantId, options => options.MapFrom(source => source));
        CreateMap<string, RuleTargetCustomerTier>()
            .ForMember(destination => destination.RuleId, options => options.Ignore())
            .ForMember(destination => destination.Rule, options => options.Ignore())
            .ForMember(destination => destination.CustomerTier, options => options.MapFrom(source => NormalizeCustomerTier(source)));
    }

    private static string NormalizeCustomerTier(string customerTier) => customerTier.Trim().ToUpperInvariant();
}