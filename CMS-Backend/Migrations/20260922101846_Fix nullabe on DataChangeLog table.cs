using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS_Backend.Migrations
{
    /// <inheritdoc />
    public partial class FixnullabeonDataChangeLogtable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_api_data_change_log_api_activity_log_log_id",
                table: "api_data_change_log");

            migrationBuilder.AlterColumn<string>(
                name: "old_value",
                table: "api_data_change_log",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<long>(
                name: "log_id",
                table: "api_data_change_log",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddForeignKey(
                name: "FK_api_data_change_log_api_activity_log_log_id",
                table: "api_data_change_log",
                column: "log_id",
                principalTable: "api_activity_log",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_api_data_change_log_api_activity_log_log_id",
                table: "api_data_change_log");

            migrationBuilder.AlterColumn<string>(
                name: "old_value",
                table: "api_data_change_log",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "log_id",
                table: "api_data_change_log",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_api_data_change_log_api_activity_log_log_id",
                table: "api_data_change_log",
                column: "log_id",
                principalTable: "api_activity_log",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
