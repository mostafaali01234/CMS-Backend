using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Addedpayrolltoloansandadjustments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "payroll_id",
                table: "employee_payroll_adjustment",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "payroll_id",
                table: "employee_loan_installment",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_employee_payroll_adjustment_payroll_id",
                table: "employee_payroll_adjustment",
                column: "payroll_id");

            migrationBuilder.CreateIndex(
                name: "IX_employee_loan_installment_payroll_id",
                table: "employee_loan_installment",
                column: "payroll_id");

            migrationBuilder.AddForeignKey(
                name: "FK_employee_loan_installment_employee_payroll_payroll_id",
                table: "employee_loan_installment",
                column: "payroll_id",
                principalTable: "employee_payroll",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_employee_payroll_adjustment_employee_payroll_payroll_id",
                table: "employee_payroll_adjustment",
                column: "payroll_id",
                principalTable: "employee_payroll",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_employee_loan_installment_employee_payroll_payroll_id",
                table: "employee_loan_installment");

            migrationBuilder.DropForeignKey(
                name: "FK_employee_payroll_adjustment_employee_payroll_payroll_id",
                table: "employee_payroll_adjustment");

            migrationBuilder.DropIndex(
                name: "IX_employee_payroll_adjustment_payroll_id",
                table: "employee_payroll_adjustment");

            migrationBuilder.DropIndex(
                name: "IX_employee_loan_installment_payroll_id",
                table: "employee_loan_installment");

            migrationBuilder.DropColumn(
                name: "payroll_id",
                table: "employee_payroll_adjustment");

            migrationBuilder.DropColumn(
                name: "payroll_id",
                table: "employee_loan_installment");
        }
    }
}
