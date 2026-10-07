using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Addedrealtionshiptoshifttable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_expense_shift_shift_id",
                table: "expense");

            migrationBuilder.DropForeignKey(
                name: "FK_money_safe_transaction_shift_shift_id",
                table: "money_safe_transaction");

            migrationBuilder.DropForeignKey(
                name: "FK_sale_invoice_shift_shift_id",
                table: "sale_invoice");

            migrationBuilder.AddColumn<long>(
                name: "ShiftId1",
                table: "sale_invoice",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ShiftId1",
                table: "money_safe_transaction",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ShiftId1",
                table: "expense",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_sale_invoice_ShiftId1",
                table: "sale_invoice",
                column: "ShiftId1");

            migrationBuilder.CreateIndex(
                name: "IX_money_safe_transaction_ShiftId1",
                table: "money_safe_transaction",
                column: "ShiftId1");

            migrationBuilder.CreateIndex(
                name: "IX_expense_ShiftId1",
                table: "expense",
                column: "ShiftId1");

            migrationBuilder.AddForeignKey(
                name: "FK_expense_shift_ShiftId1",
                table: "expense",
                column: "ShiftId1",
                principalTable: "shift",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_expense_shift_shift_id",
                table: "expense",
                column: "shift_id",
                principalTable: "shift",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_money_safe_transaction_shift_ShiftId1",
                table: "money_safe_transaction",
                column: "ShiftId1",
                principalTable: "shift",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_money_safe_transaction_shift_shift_id",
                table: "money_safe_transaction",
                column: "shift_id",
                principalTable: "shift",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_sale_invoice_shift_ShiftId1",
                table: "sale_invoice",
                column: "ShiftId1",
                principalTable: "shift",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_sale_invoice_shift_shift_id",
                table: "sale_invoice",
                column: "shift_id",
                principalTable: "shift",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_expense_shift_ShiftId1",
                table: "expense");

            migrationBuilder.DropForeignKey(
                name: "FK_expense_shift_shift_id",
                table: "expense");

            migrationBuilder.DropForeignKey(
                name: "FK_money_safe_transaction_shift_ShiftId1",
                table: "money_safe_transaction");

            migrationBuilder.DropForeignKey(
                name: "FK_money_safe_transaction_shift_shift_id",
                table: "money_safe_transaction");

            migrationBuilder.DropForeignKey(
                name: "FK_sale_invoice_shift_ShiftId1",
                table: "sale_invoice");

            migrationBuilder.DropForeignKey(
                name: "FK_sale_invoice_shift_shift_id",
                table: "sale_invoice");

            migrationBuilder.DropIndex(
                name: "IX_sale_invoice_ShiftId1",
                table: "sale_invoice");

            migrationBuilder.DropIndex(
                name: "IX_money_safe_transaction_ShiftId1",
                table: "money_safe_transaction");

            migrationBuilder.DropIndex(
                name: "IX_expense_ShiftId1",
                table: "expense");

            migrationBuilder.DropColumn(
                name: "ShiftId1",
                table: "sale_invoice");

            migrationBuilder.DropColumn(
                name: "ShiftId1",
                table: "money_safe_transaction");

            migrationBuilder.DropColumn(
                name: "ShiftId1",
                table: "expense");

            migrationBuilder.AddForeignKey(
                name: "FK_expense_shift_shift_id",
                table: "expense",
                column: "shift_id",
                principalTable: "shift",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_money_safe_transaction_shift_shift_id",
                table: "money_safe_transaction",
                column: "shift_id",
                principalTable: "shift",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_sale_invoice_shift_shift_id",
                table: "sale_invoice",
                column: "shift_id",
                principalTable: "shift",
                principalColumn: "id");
        }
    }
}
