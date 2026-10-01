using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddedProductAssembly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "product_assembly_definition",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    out_product_id = table.Column<long>(type: "bigint", nullable: false),
                    in_product_id = table.Column<long>(type: "bigint", nullable: false),
                    in_quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_assembly_definition", x => x.id);
                    table.ForeignKey(
                        name: "FK_product_assembly_definition_product_in_product_id",
                        column: x => x.in_product_id,
                        principalTable: "product",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_product_assembly_definition_product_out_product_id",
                        column: x => x.out_product_id,
                        principalTable: "product",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "product_assembly_operation",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    out_product_id = table.Column<long>(type: "bigint", nullable: false),
                    in_store_id = table.Column<long>(type: "bigint", nullable: false),
                    out_store_id = table.Column<long>(type: "bigint", nullable: false),
                    quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    operation_date = table.Column<DateTime>(type: "datetime2", nullable: false),
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
                    table.PrimaryKey("PK_product_assembly_operation", x => x.id);
                    table.ForeignKey(
                        name: "FK_product_assembly_operation_product_out_product_id",
                        column: x => x.out_product_id,
                        principalTable: "product",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_product_assembly_operation_store_in_store_id",
                        column: x => x.in_store_id,
                        principalTable: "store",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_product_assembly_operation_store_out_store_id",
                        column: x => x.out_store_id,
                        principalTable: "store",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_product_assembly_definition_in_product_id",
                table: "product_assembly_definition",
                column: "in_product_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_assembly_definition_out_product_id",
                table: "product_assembly_definition",
                column: "out_product_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_assembly_operation_in_store_id",
                table: "product_assembly_operation",
                column: "in_store_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_assembly_operation_out_product_id",
                table: "product_assembly_operation",
                column: "out_product_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_assembly_operation_out_store_id",
                table: "product_assembly_operation",
                column: "out_store_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_assembly_definition");

            migrationBuilder.DropTable(
                name: "product_assembly_operation");
        }
    }
}
