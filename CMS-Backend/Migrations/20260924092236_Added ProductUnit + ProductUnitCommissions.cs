using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddedProductUnitProductUnitCommissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "product_unit",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_unit", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "product_unit_commission",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    product_id = table.Column<long>(type: "bigint", nullable: false),
                    unit_id = table.Column<long>(type: "bigint", nullable: false),
                    sales_wholesale_comm_percent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    sales_half_wholesale_comm_percent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    sales_retail_comm_percent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    tech_comm = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    assistant_1_comm = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    assistant_2_comm = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    assistant_3_comm = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_unit_commission", x => x.id);
                    table.ForeignKey(
                        name: "FK_product_unit_commission_product_product_id",
                        column: x => x.product_id,
                        principalTable: "product",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_product_unit_commission_product_unit_unit_id",
                        column: x => x.unit_id,
                        principalTable: "product_unit",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_product_unit_commission_product_id",
                table: "product_unit_commission",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_unit_commission_unit_id",
                table: "product_unit_commission",
                column: "unit_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_unit_commission");

            migrationBuilder.DropTable(
                name: "product_unit");
        }
    }
}
