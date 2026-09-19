using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FruitShop.Api.Migrations
{
    /// <inheritdoc />
    public partial class RenameSeededPriceRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE PriceRules
                SET Name = 'Retail 10% Quantity Discount (3+)'
                WHERE Name = 'Retail Citrus Bulk Discount';

                UPDATE PriceRules
                SET Name = 'VIP 5% Discount'
                WHERE Name = 'VIP Discount';

                UPDATE PriceRules
                SET Name = 'Wholesale $1.25 Quantity Price Override (10+)'
                WHERE Name = 'Wholesale Avocado Override';

                UPDATE PriceRules
                SET Name = 'Autumn $2.99 Price Override'
                WHERE Name = 'Honeycrisp Apples Season Sale';

                UPDATE PriceRules
                SET Name = '10% Quantity Discount (5+)'
                WHERE Name = 'Honeycrisp Apples Volume Discount';

                UPDATE PriceRules
                SET Name = '10% Quantity Discount (2+)'
                WHERE Name = 'Cherry Volume Discount';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE PriceRules
                SET Name = 'Retail Citrus Bulk Discount'
                WHERE Name = 'Retail 10% Quantity Discount (3+)';

                UPDATE PriceRules
                SET Name = 'VIP Discount'
                WHERE Name = 'VIP 5% Discount';

                UPDATE PriceRules
                SET Name = 'Wholesale Avocado Override'
                WHERE Name = 'Wholesale $1.25 Quantity Price Override (10+)';

                UPDATE PriceRules
                SET Name = 'Honeycrisp Apples Season Sale'
                WHERE Name = 'Autumn $2.99 Price Override';

                UPDATE PriceRules
                SET Name = 'Honeycrisp Apples Volume Discount'
                WHERE Name = '10% Quantity Discount (5+)';

                UPDATE PriceRules
                SET Name = 'Cherry Volume Discount'
                WHERE Name = '10% Quantity Discount (2+)';
                """);
        }
    }
}
