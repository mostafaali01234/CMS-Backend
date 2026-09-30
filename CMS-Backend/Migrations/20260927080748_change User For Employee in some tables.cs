using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Api.Migrations
{
    /// <inheritdoc />
    public partial class changeUserForEmployeeinsometables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_customer_AspNetUsers_seller_id",
                table: "customer");

            migrationBuilder.AlterColumn<long>(
                name: "seller_id",
                table: "customer",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<string>(
                name: "user_id",
                table: "api_data_change_log",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "car",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    notes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    city_id = table.Column<long>(type: "bigint", nullable: false),
                    plat_number = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    model = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    oil_change_rate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    filter_change_rate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    sk_change_rate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    tire_change_rate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    license_start_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    license_end_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    insurance_start_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    insurance_end_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    insurance_company_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    owner_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    motor_number = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    chassis_number = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_car", x => x.id);
                    table.ForeignKey(
                        name: "FK_car_employee_city_id",
                        column: x => x.city_id,
                        principalTable: "employee",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_car_city_id",
                table: "car",
                column: "city_id");

            migrationBuilder.AddForeignKey(
                name: "FK_customer_employee_seller_id",
                table: "customer",
                column: "seller_id",
                principalTable: "employee",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_customer_employee_seller_id",
                table: "customer");

            migrationBuilder.DropTable(
                name: "car");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "api_data_change_log");

            migrationBuilder.AlterColumn<string>(
                name: "seller_id",
                table: "customer",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_customer_AspNetUsers_seller_id",
                table: "customer",
                column: "seller_id",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
