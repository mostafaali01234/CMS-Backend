using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AssemblyStockAndCommissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "quantity",
                table: "product_assembly_operation",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AddColumn<long>(
                name: "first_tech_id",
                table: "product_assembly_operation",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "operation_type",
                table: "product_assembly_operation",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "second_tech_id",
                table: "product_assembly_operation",
                type: "bigint",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "in_quantity",
                table: "product_assembly_definition",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AddColumn<decimal>(
                name: "first_tech_comm",
                table: "product_assembly_definition",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "operation_type",
                table: "product_assembly_definition",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "second_tech_comm",
                table: "product_assembly_definition",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<long>(
                name: "order_id",
                table: "employee_commission",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<long>(
                name: "invoice_id",
                table: "employee_commission",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<long>(
                name: "assembly_operation_id",
                table: "employee_commission",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "product_assembly_operation_item",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    operation_id = table.Column<long>(type: "bigint", nullable: false),
                    product_id = table.Column<long>(type: "bigint", nullable: false),
                    unit_quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_assembly_operation_item", x => x.id);
                    table.ForeignKey(
                        name: "FK_product_assembly_operation_item_product_assembly_operation_operation_id",
                        column: x => x.operation_id,
                        principalTable: "product_assembly_operation",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_product_assembly_operation_item_product_product_id",
                        column: x => x.product_id,
                        principalTable: "product",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_product_assembly_operation_first_tech_id",
                table: "product_assembly_operation",
                column: "first_tech_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_assembly_operation_second_tech_id",
                table: "product_assembly_operation",
                column: "second_tech_id");

            migrationBuilder.CreateIndex(
                name: "IX_employee_commission_assembly_operation_id",
                table: "employee_commission",
                column: "assembly_operation_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_assembly_operation_item_operation_id",
                table: "product_assembly_operation_item",
                column: "operation_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_assembly_operation_item_product_id",
                table: "product_assembly_operation_item",
                column: "product_id");

            migrationBuilder.AddForeignKey(
                name: "FK_employee_commission_product_assembly_operation_assembly_operation_id",
                table: "employee_commission",
                column: "assembly_operation_id",
                principalTable: "product_assembly_operation",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_product_assembly_operation_employee_first_tech_id",
                table: "product_assembly_operation",
                column: "first_tech_id",
                principalTable: "employee",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_product_assembly_operation_employee_second_tech_id",
                table: "product_assembly_operation",
                column: "second_tech_id",
                principalTable: "employee",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_employee_commission_product_assembly_operation_assembly_operation_id",
                table: "employee_commission");

            migrationBuilder.DropForeignKey(
                name: "FK_product_assembly_operation_employee_first_tech_id",
                table: "product_assembly_operation");

            migrationBuilder.DropForeignKey(
                name: "FK_product_assembly_operation_employee_second_tech_id",
                table: "product_assembly_operation");

            migrationBuilder.DropTable(
                name: "product_assembly_operation_item");

            migrationBuilder.DropIndex(
                name: "IX_product_assembly_operation_first_tech_id",
                table: "product_assembly_operation");

            migrationBuilder.DropIndex(
                name: "IX_product_assembly_operation_second_tech_id",
                table: "product_assembly_operation");

            migrationBuilder.DropIndex(
                name: "IX_employee_commission_assembly_operation_id",
                table: "employee_commission");

            migrationBuilder.DropColumn(
                name: "first_tech_id",
                table: "product_assembly_operation");

            migrationBuilder.DropColumn(
                name: "operation_type",
                table: "product_assembly_operation");

            migrationBuilder.DropColumn(
                name: "second_tech_id",
                table: "product_assembly_operation");

            migrationBuilder.DropColumn(
                name: "first_tech_comm",
                table: "product_assembly_definition");

            migrationBuilder.DropColumn(
                name: "operation_type",
                table: "product_assembly_definition");

            migrationBuilder.DropColumn(
                name: "second_tech_comm",
                table: "product_assembly_definition");

            migrationBuilder.DropColumn(
                name: "assembly_operation_id",
                table: "employee_commission");

            migrationBuilder.AlterColumn<decimal>(
                name: "quantity",
                table: "product_assembly_operation",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,3)",
                oldPrecision: 18,
                oldScale: 3);

            migrationBuilder.AlterColumn<decimal>(
                name: "in_quantity",
                table: "product_assembly_definition",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,3)",
                oldPrecision: 18,
                oldScale: 3);

            migrationBuilder.AlterColumn<long>(
                name: "order_id",
                table: "employee_commission",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "invoice_id",
                table: "employee_commission",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);
        }
    }
}
