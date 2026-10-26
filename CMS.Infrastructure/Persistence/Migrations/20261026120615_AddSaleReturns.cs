using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSaleReturns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "original_item_id",
                table: "sale_invoice_item",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_sale_invoice_item_original_item_id",
                table: "sale_invoice_item",
                column: "original_item_id");

            migrationBuilder.AddForeignKey(
                name: "FK_sale_invoice_item_sale_invoice_item_original_item_id",
                table: "sale_invoice_item",
                column: "original_item_id",
                principalTable: "sale_invoice_item",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_sale_invoice_item_sale_invoice_item_original_item_id",
                table: "sale_invoice_item");

            migrationBuilder.DropIndex(
                name: "IX_sale_invoice_item_original_item_id",
                table: "sale_invoice_item");

            migrationBuilder.DropColumn(
                name: "original_item_id",
                table: "sale_invoice_item");
        }
    }
}
