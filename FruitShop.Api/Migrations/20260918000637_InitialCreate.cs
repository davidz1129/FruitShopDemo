using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FruitShop.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CustomerTier = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, defaultValue: "RETAIL"),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PriceRules",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ParentRuleId = table.Column<long>(type: "INTEGER", nullable: true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    IsStackable = table.Column<bool>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceRules", x => x.Id);
                    table.CheckConstraint("CK_PriceRules_Status", "Status IN ('Active', 'Inactive')");
                    table.ForeignKey(
                        name: "FK_PriceRules_PriceRules_ParentRuleId",
                        column: x => x.ParentRuleId,
                        principalTable: "PriceRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UnitsOfMeasure",
                columns: table => new
                {
                    Code = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    MeasureType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitsOfMeasure", x => x.Code);
                    table.CheckConstraint("CK_UnitsOfMeasure_MeasureType", "MeasureType IN ('Weight', 'Count')");
                });

            migrationBuilder.CreateTable(
                name: "PriceRuleActions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RuleId = table.Column<long>(type: "INTEGER", nullable: false),
                    ActionType = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    CalculationBase = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false, defaultValue: "RunningTotal")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceRuleActions", x => x.Id);
                    table.CheckConstraint("CK_PriceRuleActions_ActionType", "ActionType IN ('OverridePrice', 'PercentageDiscount', 'AmountOff')");
                    table.CheckConstraint("CK_PriceRuleActions_CalculationBase", "CalculationBase IN ('OriginalBase', 'RunningTotal')");
                    table.ForeignKey(
                        name: "FK_PriceRuleActions_PriceRules_RuleId",
                        column: x => x.RuleId,
                        principalTable: "PriceRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PriceRuleConditions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RuleId = table.Column<long>(type: "INTEGER", nullable: false),
                    Attribute = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Operator = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceRuleConditions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PriceRuleConditions_PriceRules_RuleId",
                        column: x => x.RuleId,
                        principalTable: "PriceRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RuleTargetCustomerTiers",
                columns: table => new
                {
                    RuleId = table.Column<long>(type: "INTEGER", nullable: false),
                    CustomerTier = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleTargetCustomerTiers", x => new { x.RuleId, x.CustomerTier });
                    table.ForeignKey(
                        name: "FK_RuleTargetCustomerTiers_PriceRules_RuleId",
                        column: x => x.RuleId,
                        principalTable: "PriceRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductVariants",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProductId = table.Column<long>(type: "INTEGER", nullable: false),
                    Sku = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    UomCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    UomFactor = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false, defaultValue: 1.0000m),
                    BasePrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVariants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductVariants_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductVariants_UnitsOfMeasure_UomCode",
                        column: x => x.UomCode,
                        principalTable: "UnitsOfMeasure",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrderItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OrderId = table.Column<long>(type: "INTEGER", nullable: false),
                    VariantId = table.Column<long>(type: "INTEGER", nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    UnitPriceApplied = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    TotalLineAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderItems_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderItems_ProductVariants_VariantId",
                        column: x => x.VariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RuleTargetVariants",
                columns: table => new
                {
                    RuleId = table.Column<long>(type: "INTEGER", nullable: false),
                    VariantId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleTargetVariants", x => new { x.RuleId, x.VariantId });
                    table.ForeignKey(
                        name: "FK_RuleTargetVariants_PriceRules_RuleId",
                        column: x => x.RuleId,
                        principalTable: "PriceRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RuleTargetVariants_ProductVariants_VariantId",
                        column: x => x.VariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_OrderId",
                table: "OrderItems",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_VariantId",
                table: "OrderItems",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceRuleActions_RuleId",
                table: "PriceRuleActions",
                column: "RuleId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceRuleConditions_RuleId",
                table: "PriceRuleConditions",
                column: "RuleId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceRules_ParentRuleId",
                table: "PriceRules",
                column: "ParentRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_ProductId",
                table: "ProductVariants",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_Sku",
                table: "ProductVariants",
                column: "Sku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_UomCode",
                table: "ProductVariants",
                column: "UomCode");

            migrationBuilder.CreateIndex(
                name: "IX_RuleTargetVariants_VariantId",
                table: "RuleTargetVariants",
                column: "VariantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderItems");

            migrationBuilder.DropTable(
                name: "PriceRuleActions");

            migrationBuilder.DropTable(
                name: "PriceRuleConditions");

            migrationBuilder.DropTable(
                name: "RuleTargetCustomerTiers");

            migrationBuilder.DropTable(
                name: "RuleTargetVariants");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "PriceRules");

            migrationBuilder.DropTable(
                name: "ProductVariants");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "UnitsOfMeasure");
        }
    }
}
