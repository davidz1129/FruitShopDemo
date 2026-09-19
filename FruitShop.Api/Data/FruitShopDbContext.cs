using FruitShop.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FruitShop.Api.Data;

public class FruitShopDbContext(DbContextOptions<FruitShopDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<UnitOfMeasure> UnitsOfMeasure => Set<UnitOfMeasure>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<PriceRule> PriceRules => Set<PriceRule>();
    public DbSet<PriceRuleCondition> PriceRuleConditions => Set<PriceRuleCondition>();
    public DbSet<PriceRuleAction> PriceRuleActions => Set<PriceRuleAction>();
    public DbSet<RuleTargetVariant> RuleTargetVariants => Set<RuleTargetVariant>();
    public DbSet<RuleTargetCustomerTier> RuleTargetCustomerTiers => Set<RuleTargetCustomerTier>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        SetAuditTimestamps();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        SetAuditTimestamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UnitOfMeasure>(entity =>
        {
            entity.HasKey(unit => unit.Code);
            entity.Property(unit => unit.Code).HasMaxLength(20);
            entity.Property(unit => unit.MeasureType).HasConversion<string>().HasMaxLength(20);
            entity.ToTable(table => table.HasCheckConstraint("CK_UnitsOfMeasure_MeasureType", "MeasureType IN ('Weight', 'Count')"));
        });

        modelBuilder.Entity<Product>(entity => entity.Property(product => product.Name).HasMaxLength(255));

        modelBuilder.Entity<ProductVariant>(entity =>
        {
            entity.HasIndex(variant => variant.Sku).IsUnique();
            entity.Property(variant => variant.Sku).HasMaxLength(100);
            entity.Property(variant => variant.UomCode).HasMaxLength(20);
            entity.Property(variant => variant.UomFactor).HasPrecision(18, 4).HasDefaultValue(1.0000m);
            entity.Property(variant => variant.BasePrice).HasPrecision(18, 4);
            entity.Property(variant => variant.IsActive).HasDefaultValue(true);
            entity.HasOne(variant => variant.Product).WithMany(product => product.Variants)
                .HasForeignKey(variant => variant.ProductId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(variant => variant.UnitOfMeasure).WithMany()
                .HasForeignKey(variant => variant.UomCode).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.Property(order => order.CustomerTier).HasMaxLength(50).HasDefaultValue("RETAIL");
            entity.Property(order => order.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(order => order.UpdateAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(order => order.Status).HasConversion<string>().HasMaxLength(20).HasDefaultValue(OrderStatus.Pending);
            entity.Property(order => order.TotalAmount).HasPrecision(18, 4).HasDefaultValue(0m);
            entity.ToTable(table => table.HasCheckConstraint("CK_Orders_Status", "Status IN ('Pending', 'Submitted')"));
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.Property(item => item.Quantity).HasPrecision(18, 4);
            entity.Property(item => item.UnitPriceApplied).HasPrecision(18, 4);
            entity.Property(item => item.TotalLineAmount).HasPrecision(18, 4);
            entity.Property(item => item.CreateAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(item => item.UpdateAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(item => item.PriceChangeReason).HasMaxLength(1000).HasDefaultValue("Base price applied");
            entity.HasOne(item => item.Order).WithMany(order => order.Items)
                .HasForeignKey(item => item.OrderId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.Variant).WithMany()
                .HasForeignKey(item => item.VariantId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PriceRule>(entity =>
        {
            entity.Property(rule => rule.Name).HasMaxLength(255);
            entity.Property(rule => rule.AppliesToAllVariants).HasDefaultValue(false);
            entity.Property(rule => rule.Status).HasConversion<string>().HasMaxLength(20).HasDefaultValue(RuleStatus.Active);
            entity.ToTable(table => table.HasCheckConstraint("CK_PriceRules_Status", "Status IN ('Active', 'Inactive')"));
        });

        modelBuilder.Entity<PriceRuleCondition>(entity =>
        {
            entity.Property(condition => condition.Attribute).HasMaxLength(50);
            entity.Property(condition => condition.Operator).HasMaxLength(10);
            entity.HasOne(condition => condition.Rule).WithMany(rule => rule.Conditions)
                .HasForeignKey(condition => condition.RuleId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PriceRuleAction>(entity =>
        {
            entity.Property(action => action.ActionType).HasConversion<string>().HasMaxLength(30);
            entity.Property(action => action.Amount).HasPrecision(18, 4);
            entity.Property(action => action.CalculationBase).HasConversion<string>().HasMaxLength(30);
            entity.HasOne(action => action.Rule).WithMany(rule => rule.Actions)
                .HasForeignKey(action => action.RuleId).OnDelete(DeleteBehavior.Cascade);
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_PriceRuleActions_ActionType", "ActionType IN ('OverridePrice', 'PercentageDiscount', 'AmountOff')");
                table.HasCheckConstraint("CK_PriceRuleActions_CalculationBase", "CalculationBase IN ('OriginalBase', 'RunningTotal')");
            });
        });

        modelBuilder.Entity<RuleTargetVariant>(entity =>
        {
            entity.HasKey(target => new { target.RuleId, target.VariantId });
            entity.HasOne(target => target.Rule).WithMany(rule => rule.TargetVariants)
                .HasForeignKey(target => target.RuleId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(target => target.Variant).WithMany(variant => variant.TargetRules)
                .HasForeignKey(target => target.VariantId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RuleTargetCustomerTier>(entity =>
        {
            entity.HasKey(target => new { target.RuleId, target.CustomerTier });
            entity.Property(target => target.CustomerTier).HasMaxLength(50);
            entity.HasOne(target => target.Rule).WithMany(rule => rule.TargetCustomerTiers)
                .HasForeignKey(target => target.RuleId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void SetAuditTimestamps()
    {
        var utcNow = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<Order>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Entity.UpdateAt = utcNow;
            }
        }

        foreach (var entry in ChangeTracker.Entries<OrderItem>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreateAt = utcNow;
                entry.Entity.UpdateAt = utcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdateAt = utcNow;
            }
        }
    }
}