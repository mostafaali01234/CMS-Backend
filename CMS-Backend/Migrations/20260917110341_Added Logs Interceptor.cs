using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddedLogsInterceptor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApiActivityLog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    endpoint = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    http_method = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    request_body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    response_body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    status_code = table.Column<int>(type: "int", nullable: false),
                    execution_time_ms = table.Column<int>(type: "int", nullable: false),
                    ip_address = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    user_agent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    created_date = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiActivityLog", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ApiDataChangeLog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    log_id = table.Column<long>(type: "bigint", nullable: false),
                    table_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    primary_key_value = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    field_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    old_value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    new_value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    changed_date = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiDataChangeLog", x => x.id);
                    table.ForeignKey(
                        name: "FK_ApiDataChangeLog_ApiActivityLog_log_id",
                        column: x => x.log_id,
                        principalTable: "ApiActivityLog",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApiDataChangeLog_log_id",
                table: "ApiDataChangeLog",
                column: "log_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApiDataChangeLog");

            migrationBuilder.DropTable(
                name: "ApiActivityLog");
        }
    }
}
