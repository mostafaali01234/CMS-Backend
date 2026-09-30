using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddedOrderLineExpenseCategoryExpenseType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_car_employee_city_id",
                table: "car");

            migrationBuilder.RenameColumn(
                name: "city_id",
                table: "car",
                newName: "driver_id");

            migrationBuilder.RenameIndex(
                name: "IX_car_city_id",
                table: "car",
                newName: "IX_car_driver_id");

            migrationBuilder.CreateTable(
                name: "expense_category",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_expense_category", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "order_line",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    notes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    manager_id = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_line", x => x.id);
                    table.ForeignKey(
                        name: "FK_order_line_employee_manager_id",
                        column: x => x.manager_id,
                        principalTable: "employee",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "expense_type",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    category_id = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_expense_type", x => x.id);
                    table.ForeignKey(
                        name: "FK_expense_type_expense_category_category_id",
                        column: x => x.category_id,
                        principalTable: "expense_category",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "order_line_city",
                columns: table => new
                {
                    city_id = table.Column<long>(type: "bigint", nullable: false),
                    order_line_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_line_city", x => new { x.city_id, x.order_line_id });
                    table.ForeignKey(
                        name: "FK_order_line_city_city_city_id",
                        column: x => x.city_id,
                        principalTable: "city",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_order_line_city_order_line_order_line_id",
                        column: x => x.order_line_id,
                        principalTable: "order_line",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "order_line_product_category",
                columns: table => new
                {
                    order_line_id = table.Column<long>(type: "bigint", nullable: false),
                    product_category_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_line_product_category", x => new { x.order_line_id, x.product_category_id });
                    table.ForeignKey(
                        name: "FK_order_line_product_category_order_line_order_line_id",
                        column: x => x.order_line_id,
                        principalTable: "order_line",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_order_line_product_category_product_category_product_category_id",
                        column: x => x.product_category_id,
                        principalTable: "product_category",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_expense_type_category_id",
                table: "expense_type",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_line_manager_id",
                table: "order_line",
                column: "manager_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_line_city_order_line_id",
                table: "order_line_city",
                column: "order_line_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_line_product_category_product_category_id",
                table: "order_line_product_category",
                column: "product_category_id");

            migrationBuilder.AddForeignKey(
                name: "FK_car_employee_driver_id",
                table: "car",
                column: "driver_id",
                principalTable: "employee",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_car_employee_driver_id",
                table: "car");

            migrationBuilder.DropTable(
                name: "expense_type");

            migrationBuilder.DropTable(
                name: "order_line_city");

            migrationBuilder.DropTable(
                name: "order_line_product_category");

            migrationBuilder.DropTable(
                name: "expense_category");

            migrationBuilder.DropTable(
                name: "order_line");

            migrationBuilder.RenameColumn(
                name: "driver_id",
                table: "car",
                newName: "city_id");

            migrationBuilder.RenameIndex(
                name: "IX_car_driver_id",
                table: "car",
                newName: "IX_car_city_id");

            migrationBuilder.AddForeignKey(
                name: "FK_car_employee_city_id",
                table: "car",
                column: "city_id",
                principalTable: "employee",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
