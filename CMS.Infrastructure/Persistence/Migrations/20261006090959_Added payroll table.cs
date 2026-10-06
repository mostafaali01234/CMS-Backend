using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Addedpayrolltable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "employee_payroll",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    employee_id = table.Column<long>(type: "bigint", nullable: false),
                    month = table.Column<int>(type: "int", nullable: false),
                    year = table.Column<int>(type: "int", nullable: false),
                    base_salary = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    sales_commission_total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    tech_commission_total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    loans_total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    bonus_total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    deductions_total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    lunch_total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    net_total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    revised_by_id = table.Column<long>(type: "bigint", nullable: true),
                    money_safe_id = table.Column<long>(type: "bigint", nullable: true),
                    withdrawn_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    withdrawn_by_id = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_payroll", x => x.id);
                    table.ForeignKey(
                        name: "FK_employee_payroll_employee_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employee",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_employee_payroll_employee_revised_by_id",
                        column: x => x.revised_by_id,
                        principalTable: "employee",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_employee_payroll_employee_withdrawn_by_id",
                        column: x => x.withdrawn_by_id,
                        principalTable: "employee",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_employee_payroll_money_safe_money_safe_id",
                        column: x => x.money_safe_id,
                        principalTable: "money_safe",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_employee_payroll_employee_id",
                table: "employee_payroll",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_employee_payroll_money_safe_id",
                table: "employee_payroll",
                column: "money_safe_id");

            migrationBuilder.CreateIndex(
                name: "IX_employee_payroll_revised_by_id",
                table: "employee_payroll",
                column: "revised_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_employee_payroll_withdrawn_by_id",
                table: "employee_payroll",
                column: "withdrawn_by_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "employee_payroll");
        }
    }
}
