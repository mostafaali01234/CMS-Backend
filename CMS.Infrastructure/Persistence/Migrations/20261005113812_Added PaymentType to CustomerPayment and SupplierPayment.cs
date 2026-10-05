using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddedPaymentTypetoCustomerPaymentandSupplierPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "type",
                table: "supplier_payment",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "advance_payment_order_id",
                table: "customer_payment",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "type",
                table: "customer_payment",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "type",
                table: "supplier_payment");

            migrationBuilder.DropColumn(
                name: "advance_payment_order_id",
                table: "customer_payment");

            migrationBuilder.DropColumn(
                name: "type",
                table: "customer_payment");
        }
    }
}
