using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddedMoneySafeMoneySafeTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "money_safe_category",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    order_number = table.Column<long>(type: "bigint", nullable: false),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    notes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_money_safe_category", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "money_safe_type",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    cod = table.Column<long>(type: "bigint", nullable: false),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_money_safe_type", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "money_safe",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    cod = table.Column<long>(type: "bigint", nullable: false),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    notes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    active = table.Column<bool>(type: "bit", nullable: false),
                    opening_balance = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    opening_balance_currency = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    manager_id = table.Column<long>(type: "bigint", nullable: true),
                    category_id = table.Column<long>(type: "bigint", nullable: true),
                    type_id = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_money_safe", x => x.id);
                    table.ForeignKey(
                        name: "FK_money_safe_employee_manager_id",
                        column: x => x.manager_id,
                        principalTable: "employee",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_money_safe_money_safe_category_category_id",
                        column: x => x.category_id,
                        principalTable: "money_safe_category",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_money_safe_money_safe_type_type_id",
                        column: x => x.type_id,
                        principalTable: "money_safe_type",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "money_safe_transaction",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    transaction_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    transaction_notes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    transaction_amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    transaction_amount_currency = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    transaction_type = table.Column<int>(type: "int", nullable: false),
                    auditor_id = table.Column<long>(type: "bigint", nullable: false),
                    out_money_safe_id = table.Column<long>(type: "bigint", nullable: false),
                    in_money_safe_id = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("PK_money_safe_transaction", x => x.id);
                    table.ForeignKey(
                        name: "FK_money_safe_transaction_employee_auditor_id",
                        column: x => x.auditor_id,
                        principalTable: "employee",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_money_safe_transaction_money_safe_in_money_safe_id",
                        column: x => x.in_money_safe_id,
                        principalTable: "money_safe",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_money_safe_transaction_money_safe_out_money_safe_id",
                        column: x => x.out_money_safe_id,
                        principalTable: "money_safe",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_money_safe_transaction_project_project_id",
                        column: x => x.project_id,
                        principalTable: "project",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_money_safe_category_id",
                table: "money_safe",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_money_safe_manager_id",
                table: "money_safe",
                column: "manager_id");

            migrationBuilder.CreateIndex(
                name: "IX_money_safe_type_id",
                table: "money_safe",
                column: "type_id");

            migrationBuilder.CreateIndex(
                name: "IX_money_safe_transaction_auditor_id",
                table: "money_safe_transaction",
                column: "auditor_id");

            migrationBuilder.CreateIndex(
                name: "IX_money_safe_transaction_in_money_safe_id",
                table: "money_safe_transaction",
                column: "in_money_safe_id");

            migrationBuilder.CreateIndex(
                name: "IX_money_safe_transaction_out_money_safe_id",
                table: "money_safe_transaction",
                column: "out_money_safe_id");

            migrationBuilder.CreateIndex(
                name: "IX_money_safe_transaction_project_id",
                table: "money_safe_transaction",
                column: "project_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "money_safe_transaction");

            migrationBuilder.DropTable(
                name: "money_safe");

            migrationBuilder.DropTable(
                name: "money_safe_category");

            migrationBuilder.DropTable(
                name: "money_safe_type");
        }
    }
}
