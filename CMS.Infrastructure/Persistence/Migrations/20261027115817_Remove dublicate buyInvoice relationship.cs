using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemovedublicatebuyInvoicerelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_buy_invoice_buy_invoice_BuyInvoiceId",
                table: "buy_invoice");

            migrationBuilder.DropIndex(
                name: "IX_buy_invoice_BuyInvoiceId",
                table: "buy_invoice");

            migrationBuilder.DropColumn(
                name: "BuyInvoiceId",
                table: "buy_invoice");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "BuyInvoiceId",
                table: "buy_invoice",
                type: "bigint",
                nullable: true);

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
        }
    }
}
