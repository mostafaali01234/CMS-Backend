using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddedShiftShiftTech : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "shift_id",
                table: "sale_invoice",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "shift_id",
                table: "money_safe_transaction",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "shift_id",
                table: "expense",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "shift_id",
                table: "employee_loan",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "shift",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    country_state_id = table.Column<long>(type: "bigint", nullable: false),
                    car_id = table.Column<long>(type: "bigint", nullable: false),
                    store_id = table.Column<long>(type: "bigint", nullable: false),
                    km_start = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    km_end = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
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
                    table.PrimaryKey("PK_shift", x => x.id);
                    table.ForeignKey(
                        name: "FK_shift_car_car_id",
                        column: x => x.car_id,
                        principalTable: "car",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shift_country_state_country_state_id",
                        column: x => x.country_state_id,
                        principalTable: "country_state",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shift_project_project_id",
                        column: x => x.project_id,
                        principalTable: "project",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_shift_store_store_id",
                        column: x => x.store_id,
                        principalTable: "store",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shift_tech",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    shift_id = table.Column<long>(type: "bigint", nullable: false),
                    tech_id = table.Column<long>(type: "bigint", nullable: false),
                    tech_type = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shift_tech", x => x.id);
                    table.ForeignKey(
                        name: "FK_shift_tech_employee_tech_id",
                        column: x => x.tech_id,
                        principalTable: "employee",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shift_tech_shift_shift_id",
                        column: x => x.shift_id,
                        principalTable: "shift",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_sale_invoice_shift_id",
                table: "sale_invoice",
                column: "shift_id");

            migrationBuilder.CreateIndex(
                name: "IX_money_safe_transaction_shift_id",
                table: "money_safe_transaction",
                column: "shift_id");

            migrationBuilder.CreateIndex(
                name: "IX_expense_shift_id",
                table: "expense",
                column: "shift_id");

            migrationBuilder.CreateIndex(
                name: "IX_employee_loan_shift_id",
                table: "employee_loan",
                column: "shift_id");

            migrationBuilder.CreateIndex(
                name: "IX_shift_car_id",
                table: "shift",
                column: "car_id");

            migrationBuilder.CreateIndex(
                name: "IX_shift_country_state_id",
                table: "shift",
                column: "country_state_id");

            migrationBuilder.CreateIndex(
                name: "IX_shift_project_id",
                table: "shift",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "IX_shift_store_id",
                table: "shift",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "IX_shift_tech_shift_id",
                table: "shift_tech",
                column: "shift_id");

            migrationBuilder.CreateIndex(
                name: "IX_shift_tech_tech_id",
                table: "shift_tech",
                column: "tech_id");

            migrationBuilder.AddForeignKey(
                name: "FK_employee_loan_shift_shift_id",
                table: "employee_loan",
                column: "shift_id",
                principalTable: "shift",
                principalColumn: "id");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_employee_loan_shift_shift_id",
                table: "employee_loan");

            migrationBuilder.DropForeignKey(
                name: "FK_expense_shift_shift_id",
                table: "expense");

            migrationBuilder.DropForeignKey(
                name: "FK_money_safe_transaction_shift_shift_id",
                table: "money_safe_transaction");

            migrationBuilder.DropForeignKey(
                name: "FK_sale_invoice_shift_shift_id",
                table: "sale_invoice");

            migrationBuilder.DropTable(
                name: "shift_tech");

            migrationBuilder.DropTable(
                name: "shift");

            migrationBuilder.DropIndex(
                name: "IX_sale_invoice_shift_id",
                table: "sale_invoice");

            migrationBuilder.DropIndex(
                name: "IX_money_safe_transaction_shift_id",
                table: "money_safe_transaction");

            migrationBuilder.DropIndex(
                name: "IX_expense_shift_id",
                table: "expense");

            migrationBuilder.DropIndex(
                name: "IX_employee_loan_shift_id",
                table: "employee_loan");

            migrationBuilder.DropColumn(
                name: "shift_id",
                table: "sale_invoice");

            migrationBuilder.DropColumn(
                name: "shift_id",
                table: "money_safe_transaction");

            migrationBuilder.DropColumn(
                name: "shift_id",
                table: "expense");

            migrationBuilder.DropColumn(
                name: "shift_id",
                table: "employee_loan");
        }
    }
}
