using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddedColumnsformanytablesforsalary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "manager_commission_total",
                table: "employee_payroll",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<long>(
                name: "payroll_id",
                table: "employee_commission",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_employee_commission_payroll_id",
                table: "employee_commission",
                column: "payroll_id");

            migrationBuilder.AddForeignKey(
                name: "FK_employee_commission_employee_payroll_payroll_id",
                table: "employee_commission",
                column: "payroll_id",
                principalTable: "employee_payroll",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_employee_commission_employee_payroll_payroll_id",
                table: "employee_commission");

            migrationBuilder.DropIndex(
                name: "IX_employee_commission_payroll_id",
                table: "employee_commission");

            migrationBuilder.DropColumn(
                name: "manager_commission_total",
                table: "employee_payroll");

            migrationBuilder.DropColumn(
                name: "payroll_id",
                table: "employee_commission");
        }
    }
}
