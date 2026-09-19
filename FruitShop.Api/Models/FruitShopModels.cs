using System.Text.Json.Serialization;

namespace FruitShop.Api.Models;

public enum MeasureType
{
    Weight,
    Count
}

public enum RuleStatus
{
    Active,
    Inactive
}

public enum OrderStatus
{
    Pending,
    Submitted
}

public enum ActionType
{
    OverridePrice,
    PercentageDiscount,
    AmountOff
}

public enum CalculationBase
{
    OriginalBase,
    RunningTotal
}

public class Product
{
    public long Id { get; set; }
    public required string Name { get; set; }
    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
}

public class UnitOfMeasure
{
    public required string Code { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public MeasureType MeasureType { get; set; }
}

public class ProductVariant
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public required string Sku { get; set; }
    public required string UomCode { get; set; }
    public decimal UomFactor { get; set; } = 1.0000m;
    public decimal BasePrice { get; set; }
    public bool IsActive { get; set; } = true;
    [JsonIgnore]
    public Product Product { get; set; } = null!;
    public UnitOfMeasure UnitOfMeasure { get; set; } = null!;
    public ICollection<RuleTargetVariant> TargetRules { get; set; } = new List<RuleTargetVariant>();
}

public class Order
{
    public long Id { get; set; }
    public required string CustomerTier { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdateAt { get; set; }
    public OrderStatus Status { get; private set; } = OrderStatus.Pending;
    public decimal TotalAmount { get; private set; }
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();

    public void AddItem(
        ProductVariant variant,
        decimal quantity,
        decimal unitPriceApplied,
        string priceChangeReason = "Base price applied")
    {
        ArgumentNullException.ThrowIfNull(variant);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        ArgumentOutOfRangeException.ThrowIfNegative(unitPriceApplied);
        ArgumentException.ThrowIfNullOrWhiteSpace(priceChangeReason);

        EnsurePending();

        var existingItem = Items.FirstOrDefault(item => item.VariantId == variant.Id);
        if (existingItem is not null)
        {
            existingItem.Quantity = quantity;
            existingItem.UnitPriceApplied = unitPriceApplied;
            existingItem.TotalLineAmount = Math.Round(quantity * unitPriceApplied, 2, MidpointRounding.AwayFromZero);
            existingItem.PriceChangeReason = priceChangeReason;
            RecalculateTotalAmount();
            return;
        }

        Items.Add(new OrderItem
        {
            Order = this,
            OrderId = Id,
            Variant = variant,
            VariantId = variant.Id,
            Quantity = quantity,
            UnitPriceApplied = unitPriceApplied,
            TotalLineAmount = Math.Round(quantity * unitPriceApplied, 2, MidpointRounding.AwayFromZero),
            PriceChangeReason = priceChangeReason
        });

        RecalculateTotalAmount();
    }

    public void Submit()
    {
        EnsurePending();
        Status = OrderStatus.Submitted;
    }

    private void RecalculateTotalAmount() =>
        TotalAmount = Items.Sum(item => item.TotalLineAmount);

    private void EnsurePending()
    {
        if (Status == OrderStatus.Submitted)
        {
            throw new InvalidOperationException("Submitted orders cannot be changed.");
        }
    }
}

public class OrderItem
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public long VariantId { get; set; }
    public DateTime CreateAt { get; set; }
    public DateTime UpdateAt { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPriceApplied { get; set; }
    public decimal TotalLineAmount { get; set; }
    public string PriceChangeReason { get; set; } = "Base price applied";
    [JsonIgnore]
    public Order Order { get; set; } = null!;
    public ProductVariant Variant { get; set; } = null!;
}

public class PriceRule
{
    public long Id { get; set; }
    public required string Name { get; set; }
    public int Priority { get; set; }
    public bool IsStackable { get; set; }
    public bool AppliesToAllVariants { get; set; }
    public RuleStatus Status { get; set; } = RuleStatus.Active;
    public ICollection<PriceRuleCondition> Conditions { get; set; } = new List<PriceRuleCondition>();
    public ICollection<PriceRuleAction> Actions { get; set; } = new List<PriceRuleAction>();
    [JsonIgnore]
    public ICollection<RuleTargetVariant> TargetVariants { get; set; } = new List<RuleTargetVariant>();
    public ICollection<RuleTargetCustomerTier> TargetCustomerTiers { get; set; } = new List<RuleTargetCustomerTier>();
}

public class PriceRuleCondition
{
    public long Id { get; set; }
    public long RuleId { get; set; }
    public required string Attribute { get; set; }
    public required string Operator { get; set; }
    public required string Value { get; set; }
    [JsonIgnore]
    public PriceRule Rule { get; set; } = null!;
}

public class PriceRuleAction
{
    public long Id { get; set; }
    public long RuleId { get; set; }
    public ActionType ActionType { get; set; }
    public decimal Amount { get; set; }
    public CalculationBase CalculationBase { get; set; } = CalculationBase.RunningTotal;
    [JsonIgnore]
    public PriceRule Rule { get; set; } = null!;
}

public class RuleTargetVariant
{
    public long RuleId { get; set; }
    public long VariantId { get; set; }
    public PriceRule Rule { get; set; } = null!;
    [JsonIgnore]
    public ProductVariant Variant { get; set; } = null!;
}

public class RuleTargetCustomerTier
{
    public long RuleId { get; set; }
    public required string CustomerTier { get; set; }
    [JsonIgnore]
    public PriceRule Rule { get; set; } = null!;
}