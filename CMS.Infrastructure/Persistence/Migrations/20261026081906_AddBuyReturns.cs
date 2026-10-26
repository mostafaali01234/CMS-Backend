using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBuyReturns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "original_item_id",
                table: "buy_invoice_item",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "BuyInvoiceId",
                table: "buy_invoice",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_buy_invoice_item_original_item_id",
                table: "buy_invoice_item",
                column: "original_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_buy_invoice_BuyInvoiceId",
                table: "buy_invoice",
                column: "BuyInvoiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_buy_invoice_buy_invoice_BuyInvoiceId",
                table: "buy_invoice",
                column: "BuyInvoiceId",
                principalTable: "buy_invoice",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_buy_invoice_item_buy_invoice_item_original_item_id",
                table: "buy_invoice_item",
                column: "original_item_id",
                principalTable: "buy_invoice_item",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_buy_invoice_buy_invoice_BuyInvoiceId",
                table: "buy_invoice");

            migrationBuilder.DropForeignKey(
                name: "FK_buy_invoice_item_buy_invoice_item_original_item_id",
                table: "buy_invoice_item");

            migrationBuilder.DropIndex(
                name: "IX_buy_invoice_item_original_item_id",
                table: "buy_invoice_item");

            migrationBuilder.DropIndex(
                name: "IX_buy_invoice_BuyInvoiceId",
                table: "buy_invoice");

            migrationBuilder.DropColumn(
                name: "original_item_id",
                table: "buy_invoice_item");

            migrationBuilder.DropColumn(
                name: "BuyInvoiceId",
                table: "buy_invoice");
        }
    }
}
