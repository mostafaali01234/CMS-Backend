using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddedExpensesEmployeeLoanandEmployeePayrollAdjustment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_employee_AspNetUsers_user_id",
                table: "employee");

            migrationBuilder.DropForeignKey(
                name: "FK_refresh_token_AspNetUsers_user_id",
                table: "refresh_token");

            migrationBuilder.CreateTable(
                name: "employee_loan",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    employee_id = table.Column<long>(type: "bigint", nullable: false),
                    approved_by_id = table.Column<long>(type: "bigint", nullable: true),
                    amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    money_safe_id = table.Column<long>(type: "bigint", nullable: true),
                    installment_count = table.Column<int>(type: "int", nullable: false),
                    start_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    notes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    project_id = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_loan", x => x.id);
                    table.ForeignKey(
                        name: "FK_employee_loan_employee_approved_by_id",
                        column: x => x.approved_by_id,
                        principalTable: "employee",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_employee_loan_employee_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employee",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_employee_loan_money_safe_money_safe_id",
                        column: x => x.money_safe_id,
                        principalTable: "money_safe",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_employee_loan_project_project_id",
                        column: x => x.project_id,
                        principalTable: "project",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "employee_payroll_adjustment",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    employee_id = table.Column<long>(type: "bigint", nullable: false),
                    operation_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    emp_kind = table.Column<int>(type: "int", nullable: false),
                    type = table.Column<int>(type: "int", nullable: false),
                    amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    approved_by_id = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_payroll_adjustment", x => x.id);
                    table.ForeignKey(
                        name: "FK_employee_payroll_adjustment_employee_approved_by_id",
                        column: x => x.approved_by_id,
                        principalTable: "employee",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_employee_payroll_adjustment_employee_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employee",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "expense",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    expense_type_id = table.Column<long>(type: "bigint", nullable: false),
                    amount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    amount_currency = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    notes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    data = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    money_safe_id = table.Column<long>(type: "bigint", nullable: false),
                    auditor_id = table.Column<long>(type: "bigint", nullable: false),
                    project_id = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_expense", x => x.id);
                    table.ForeignKey(
                        name: "FK_expense_employee_auditor_id",
                        column: x => x.auditor_id,
                        principalTable: "employee",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_expense_expense_type_expense_type_id",
                        column: x => x.expense_type_id,
                        principalTable: "expense_type",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_expense_money_safe_money_safe_id",
                        column: x => x.money_safe_id,
                        principalTable: "money_safe",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_expense_project_project_id",
                        column: x => x.project_id,
                        principalTable: "project",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "employee_loan_installment",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    loan_id = table.Column<long>(type: "bigint", nullable: false),
                    installment_number = table.Column<int>(type: "int", nullable: false),
                    due_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    deduction_id = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_loan_installment", x => x.id);
                    table.ForeignKey(
                        name: "FK_employee_loan_installment_employee_loan_loan_id",
                        column: x => x.loan_id,
                        principalTable: "employee_loan",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_employee_loan_installment_employee_payroll_adjustment_deduction_id",
                        column: x => x.deduction_id,
                        principalTable: "employee_payroll_adjustment",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_employee_loan_approved_by_id",
                table: "employee_loan",
                column: "approved_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_employee_loan_employee_id",
                table: "employee_loan",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_employee_loan_money_safe_id",
                table: "employee_loan",
                column: "money_safe_id");

            migrationBuilder.CreateIndex(
                name: "IX_employee_loan_project_id",
                table: "employee_loan",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "IX_employee_loan_installment_deduction_id",
                table: "employee_loan_installment",
                column: "deduction_id");

            migrationBuilder.CreateIndex(
                name: "IX_employee_loan_installment_loan_id",
                table: "employee_loan_installment",
                column: "loan_id");

            migrationBuilder.CreateIndex(
                name: "IX_employee_payroll_adjustment_approved_by_id",
                table: "employee_payroll_adjustment",
                column: "approved_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_employee_payroll_adjustment_employee_id",
                table: "employee_payroll_adjustment",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_expense_auditor_id",
                table: "expense",
                column: "auditor_id");

            migrationBuilder.CreateIndex(
                name: "IX_expense_expense_type_id",
                table: "expense",
                column: "expense_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_expense_money_safe_id",
                table: "expense",
                column: "money_safe_id");

            migrationBuilder.CreateIndex(
                name: "IX_expense_project_id",
                table: "expense",
                column: "project_id");

            migrationBuilder.AddForeignKey(
                name: "FK_employee_AspNetUsers_user_id",
                table: "employee",
                column: "user_id",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_refresh_token_AspNetUsers_user_id",
                table: "refresh_token",
                column: "user_id",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_employee_AspNetUsers_user_id",
                table: "employee");

            migrationBuilder.DropForeignKey(
                name: "FK_refresh_token_AspNetUsers_user_id",
                table: "refresh_token");

            migrationBuilder.DropTable(
                name: "employee_loan_installment");

            migrationBuilder.DropTable(
                name: "expense");

            migrationBuilder.DropTable(
                name: "employee_loan");

            migrationBuilder.DropTable(
                name: "employee_payroll_adjustment");

            migrationBuilder.AddForeignKey(
                name: "FK_employee_AspNetUsers_user_id",
                table: "employee",
                column: "user_id",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_refresh_token_AspNetUsers_user_id",
                table: "refresh_token",
                column: "user_id",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
