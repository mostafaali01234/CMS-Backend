using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddedEmployeetablebasictables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApiDataChangeLog_ApiActivityLog_log_id",
                table: "ApiDataChangeLog");

            migrationBuilder.DropForeignKey(
                name: "FK_RefreshTokens_AspNetUsers_UserId",
                table: "RefreshTokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RefreshTokens",
                table: "RefreshTokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ApiDataChangeLog",
                table: "ApiDataChangeLog");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ApiActivityLog",
                table: "ApiActivityLog");

            migrationBuilder.RenameTable(
                name: "RefreshTokens",
                newName: "refresh_token");

            migrationBuilder.RenameTable(
                name: "ApiDataChangeLog",
                newName: "api_data_change_log");

            migrationBuilder.RenameTable(
                name: "ApiActivityLog",
                newName: "api_activity_log");

            migrationBuilder.RenameColumn(
                name: "Token",
                table: "refresh_token",
                newName: "token");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "refresh_token",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "refresh_token",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "JwtId",
                table: "refresh_token",
                newName: "jwt_id");

            migrationBuilder.RenameColumn(
                name: "IsUsed",
                table: "refresh_token",
                newName: "is_used");

            migrationBuilder.RenameColumn(
                name: "IsRevoked",
                table: "refresh_token",
                newName: "is_revoked");

            migrationBuilder.RenameColumn(
                name: "ExpiryDate",
                table: "refresh_token",
                newName: "expiry_date");

            migrationBuilder.RenameColumn(
                name: "AddedDate",
                table: "refresh_token",
                newName: "added_date");

            migrationBuilder.RenameIndex(
                name: "IX_RefreshTokens_UserId",
                table: "refresh_token",
                newName: "IX_refresh_token_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_ApiDataChangeLog_log_id",
                table: "api_data_change_log",
                newName: "IX_api_data_change_log_log_id");

            migrationBuilder.AlterColumn<long>(
                name: "id",
                table: "refresh_token",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("SqlServer:Identity", "1, 1")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddPrimaryKey(
                name: "PK_refresh_token",
                table: "refresh_token",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_api_data_change_log",
                table: "api_data_change_log",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_api_activity_log",
                table: "api_activity_log",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_api_data_change_log_api_activity_log_log_id",
                table: "api_data_change_log",
                column: "log_id",
                principalTable: "api_activity_log",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_refresh_token_AspNetUsers_user_id",
                table: "refresh_token",
                column: "user_id",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_api_data_change_log_api_activity_log_log_id",
                table: "api_data_change_log");

            migrationBuilder.DropForeignKey(
                name: "FK_refresh_token_AspNetUsers_user_id",
                table: "refresh_token");

            migrationBuilder.DropPrimaryKey(
                name: "PK_refresh_token",
                table: "refresh_token");

            migrationBuilder.DropPrimaryKey(
                name: "PK_api_data_change_log",
                table: "api_data_change_log");

            migrationBuilder.DropPrimaryKey(
                name: "PK_api_activity_log",
                table: "api_activity_log");

            migrationBuilder.RenameTable(
                name: "refresh_token",
                newName: "RefreshTokens");

            migrationBuilder.RenameTable(
                name: "api_data_change_log",
                newName: "ApiDataChangeLog");

            migrationBuilder.RenameTable(
                name: "api_activity_log",
                newName: "ApiActivityLog");

            migrationBuilder.RenameColumn(
                name: "token",
                table: "RefreshTokens",
                newName: "Token");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "RefreshTokens",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "RefreshTokens",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "jwt_id",
                table: "RefreshTokens",
                newName: "JwtId");

            migrationBuilder.RenameColumn(
                name: "is_used",
                table: "RefreshTokens",
                newName: "IsUsed");

            migrationBuilder.RenameColumn(
                name: "is_revoked",
                table: "RefreshTokens",
                newName: "IsRevoked");

            migrationBuilder.RenameColumn(
                name: "expiry_date",
                table: "RefreshTokens",
                newName: "ExpiryDate");

            migrationBuilder.RenameColumn(
                name: "added_date",
                table: "RefreshTokens",
                newName: "AddedDate");

            migrationBuilder.RenameIndex(
                name: "IX_refresh_token_user_id",
                table: "RefreshTokens",
                newName: "IX_RefreshTokens_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_api_data_change_log_log_id",
                table: "ApiDataChangeLog",
                newName: "IX_ApiDataChangeLog_log_id");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "RefreshTokens",
                type: "int",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .Annotation("SqlServer:Identity", "1, 1")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RefreshTokens",
                table: "RefreshTokens",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ApiDataChangeLog",
                table: "ApiDataChangeLog",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ApiActivityLog",
                table: "ApiActivityLog",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_ApiDataChangeLog_ApiActivityLog_log_id",
                table: "ApiDataChangeLog",
                column: "log_id",
                principalTable: "ApiActivityLog",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RefreshTokens_AspNetUsers_UserId",
                table: "RefreshTokens",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
