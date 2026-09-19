using AutoMapper;
using FruitShop.Api.Features.Orders;
using FruitShop.Api.Mapping;
using FruitShop.Api.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace FruitShop.Api.Tests;

public sealed class FruitShopMappingProfileTests
{
    private static readonly MapperConfiguration Configuration = new(
        options => options.AddProfile<FruitShopMappingProfile>(),
        NullLoggerFactory.Instance);

    [Fact]
    public void Configuration_is_valid()
    {
        Configuration.AssertConfigurationIsValid();
    }

    [Fact]
    public void Maps_order_response_with_utc_timestamps_and_items_in_id_order()
    {
        var order = new Order
        {
            Id = 42,
            CustomerTier = "VIP",
            CreatedAt = new DateTime(2026, 9, 19, 9, 0, 0, DateTimeKind.Unspecified),
            UpdateAt = new DateTime(2026, 9, 19, 10, 0, 0, DateTimeKind.Unspecified),
            Items =
            [
                new OrderItem
                {
                    Id = 2,
                    VariantId = 20,
                    Quantity = 1,
                    UnitPriceApplied = 3.50m,
                    TotalLineAmount = 3.50m,
                    Variant = new ProductVariant { Sku = "SECOND", UomCode = "EA" }
                },
                new OrderItem
                {
                    Id = 1,
                    VariantId = 10,
                    Quantity = 2,
                    UnitPriceApplied = 2.25m,
                    TotalLineAmount = 4.50m,
                    Variant = new ProductVariant { Sku = "FIRST", UomCode = "KG" }
                }
            ]
        };

        var response = Configuration.CreateMapper().Map<OrderResponse>(order);

        Assert.Equal(new DateTimeOffset(2026, 9, 19, 9, 0, 0, TimeSpan.Zero), response.CreatedAt);
        Assert.Equal([1L, 2L], response.Items.Select(item => item.Id));
        Assert.Equal("FIRST", response.Items[0].Sku);
        Assert.Equal("KG", response.Items[0].UnitOfMeasure);
    }
}