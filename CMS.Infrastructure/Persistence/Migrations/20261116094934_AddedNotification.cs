using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddedNotification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "notification",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    recipient_user_id = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    type = table.Column<int>(type: "int", nullable: false),
                    title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    body = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    entity_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    entity_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    read_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_message",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    sender_user_id = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    recipient_user_id = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    order_id = table.Column<long>(type: "bigint", nullable: true),
                    body = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    read_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_message", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_message_order_order_id",
                        column: x => x.order_id,
                        principalTable: "order",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_notification_recipient_user_id_read_at_utc_created_at_utc",
                table: "notification",
                columns: new[] { "recipient_user_id", "read_at_utc", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_user_message_order_id",
                table: "user_message",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_message_recipient_user_id_sender_user_id_created_at_utc",
                table: "user_message",
                columns: new[] { "recipient_user_id", "sender_user_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_user_message_sender_user_id_recipient_user_id_created_at_utc",
                table: "user_message",
                columns: new[] { "sender_user_id", "recipient_user_id", "created_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notification");

            migrationBuilder.DropTable(
                name: "user_message");
        }
    }
}
