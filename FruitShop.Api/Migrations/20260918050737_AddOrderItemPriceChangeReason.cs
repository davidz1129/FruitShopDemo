using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FruitShop.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderItemPriceChangeReason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PriceChangeReason",
                table: "OrderItems",
                type: "TEXT",
                maxLength: 1000,
                nullable: false,
                defaultValue: "Base price applied");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PriceChangeReason",
                table: "OrderItems");
        }
    }
}
