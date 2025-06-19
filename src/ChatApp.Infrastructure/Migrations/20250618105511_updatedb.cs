using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class updatedb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ChatRolePermissions",
                table: "ChatRolePermissions");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ChatRolePermissions",
                table: "ChatRolePermissions",
                columns: new[] { "ChatId", "Role" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ChatRolePermissions",
                table: "ChatRolePermissions");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ChatRolePermissions",
                table: "ChatRolePermissions",
                column: "ChatId");
        }
    }
}
