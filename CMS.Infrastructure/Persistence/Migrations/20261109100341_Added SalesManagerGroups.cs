using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddedSalesManagerGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "manager_group_id",
                table: "employee_commission",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "source_employee_id",
                table: "employee_commission",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "sales_manager_group",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    manager_id = table.Column<long>(type: "bigint", nullable: false),
                    percent = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    active = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_sales_manager_group", x => x.id);
                    table.ForeignKey(
                        name: "FK_sales_manager_group_employee_manager_id",
                        column: x => x.manager_id,
                        principalTable: "employee",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sales_manager_group_product",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    group_id = table.Column<long>(type: "bigint", nullable: false),
                    product_id = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sales_manager_group_product", x => x.id);
                    table.ForeignKey(
                        name: "FK_sales_manager_group_product_product_product_id",
                        column: x => x.product_id,
                        principalTable: "product",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sales_manager_group_product_sales_manager_group_group_id",
                        column: x => x.group_id,
                        principalTable: "sales_manager_group",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sales_manager_group_seller",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    group_id = table.Column<long>(type: "bigint", nullable: false),
                    employee_id = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sales_manager_group_seller", x => x.id);
                    table.ForeignKey(
                        name: "FK_sales_manager_group_seller_employee_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employee",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sales_manager_group_seller_sales_manager_group_group_id",
                        column: x => x.group_id,
                        principalTable: "sales_manager_group",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_sales_manager_group_manager_id",
                table: "sales_manager_group",
                column: "manager_id",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_sales_manager_group_product_group_id_product_id",
                table: "sales_manager_group_product",
                columns: new[] { "group_id", "product_id" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_sales_manager_group_product_product_id",
                table: "sales_manager_group_product",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_manager_group_seller_employee_id",
                table: "sales_manager_group_seller",
                column: "employee_id",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_sales_manager_group_seller_group_id",
                table: "sales_manager_group_seller",
                column: "group_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sales_manager_group_product");

            migrationBuilder.DropTable(
                name: "sales_manager_group_seller");

            migrationBuilder.DropTable(
                name: "sales_manager_group");

            migrationBuilder.DropColumn(
                name: "manager_group_id",
                table: "employee_commission");

            migrationBuilder.DropColumn(
                name: "source_employee_id",
                table: "employee_commission");
        }
    }
}
