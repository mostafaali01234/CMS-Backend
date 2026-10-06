using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Addedemployee_special_commissiontables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "inspection_commission_deduction_percent",
                table: "product",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "category",
                table: "order",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "old_order_id",
                table: "order",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "employee_special_commission",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    manager_id = table.Column<long>(type: "bigint", nullable: false),
                    employee_id = table.Column<long>(type: "bigint", nullable: false),
                    month = table.Column<int>(type: "int", nullable: false),
                    year = table.Column<int>(type: "int", nullable: false),
                    sales_total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    commission_rate = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    commission_Total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
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
                    table.PrimaryKey("PK_employee_special_commission", x => x.id);
                    table.ForeignKey(
                        name: "FK_employee_special_commission_employee_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employee",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_employee_special_commission_employee_manager_id",
                        column: x => x.manager_id,
                        principalTable: "employee",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "employee_special_commission_definition",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    manager_id = table.Column<long>(type: "bigint", nullable: false),
                    commission_rate = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_special_commission_definition", x => x.id);
                    table.ForeignKey(
                        name: "FK_employee_special_commission_definition_employee_manager_id",
                        column: x => x.manager_id,
                        principalTable: "employee",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "employee_special_commission_definition_employee",
                columns: table => new
                {
                    EmployeeSpecialCommissionDefinitionId = table.Column<long>(type: "bigint", nullable: false),
                    EmployeesId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_special_commission_definition_employee", x => new { x.EmployeeSpecialCommissionDefinitionId, x.EmployeesId });
                    table.ForeignKey(
                        name: "FK_employee_special_commission_definition_employee_employee_EmployeesId",
                        column: x => x.EmployeesId,
                        principalTable: "employee",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_employee_special_commission_definition_employee_employee_special_commission_definition_EmployeeSpecialCommissionDefinitionId",
                        column: x => x.EmployeeSpecialCommissionDefinitionId,
                        principalTable: "employee_special_commission_definition",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "employee_special_commission_definition_product",
                columns: table => new
                {
                    EmployeeSpecialCommissionDefinitionId = table.Column<long>(type: "bigint", nullable: false),
                    ProductsId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_special_commission_definition_product", x => new { x.EmployeeSpecialCommissionDefinitionId, x.ProductsId });
                    table.ForeignKey(
                        name: "FK_employee_special_commission_definition_product_employee_special_commission_definition_EmployeeSpecialCommissionDefinitionId",
                        column: x => x.EmployeeSpecialCommissionDefinitionId,
                        principalTable: "employee_special_commission_definition",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_employee_special_commission_definition_product_product_ProductsId",
                        column: x => x.ProductsId,
                        principalTable: "product",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_order_old_order_id",
                table: "order",
                column: "old_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_employee_special_commission_employee_id",
                table: "employee_special_commission",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_employee_special_commission_manager_id",
                table: "employee_special_commission",
                column: "manager_id");

            migrationBuilder.CreateIndex(
                name: "IX_employee_special_commission_definition_manager_id",
                table: "employee_special_commission_definition",
                column: "manager_id");

            migrationBuilder.CreateIndex(
                name: "IX_employee_special_commission_definition_employee_EmployeesId",
                table: "employee_special_commission_definition_employee",
                column: "EmployeesId");

            migrationBuilder.CreateIndex(
                name: "IX_employee_special_commission_definition_product_ProductsId",
                table: "employee_special_commission_definition_product",
                column: "ProductsId");

            migrationBuilder.AddForeignKey(
                name: "FK_order_order_old_order_id",
                table: "order",
                column: "old_order_id",
                principalTable: "order",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_order_order_old_order_id",
                table: "order");

            migrationBuilder.DropTable(
                name: "employee_special_commission");

            migrationBuilder.DropTable(
                name: "employee_special_commission_definition_employee");

            migrationBuilder.DropTable(
                name: "employee_special_commission_definition_product");

            migrationBuilder.DropTable(
                name: "employee_special_commission_definition");

            migrationBuilder.DropIndex(
                name: "IX_order_old_order_id",
                table: "order");

            migrationBuilder.DropColumn(
                name: "inspection_commission_deduction_percent",
                table: "product");

            migrationBuilder.DropColumn(
                name: "category",
                table: "order");

            migrationBuilder.DropColumn(
                name: "old_order_id",
                table: "order");
        }
    }
}
