using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FruitShop.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemovePriceRuleParentRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PriceRules_PriceRules_ParentRuleId",
                table: "PriceRules");

            migrationBuilder.DropIndex(
                name: "IX_PriceRules_ParentRuleId",
                table: "PriceRules");

            migrationBuilder.DropColumn(
                name: "ParentRuleId",
                table: "PriceRules");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ParentRuleId",
                table: "PriceRules",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PriceRules_ParentRuleId",
                table: "PriceRules",
                column: "ParentRuleId");

            migrationBuilder.AddForeignKey(
                name: "FK_PriceRules_PriceRules_ParentRuleId",
                table: "PriceRules",
                column: "ParentRuleId",
                principalTable: "PriceRules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
