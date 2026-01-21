using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kopitra.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUsersTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Roles = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CanProvide = table.Column<bool>(type: "bit", nullable: false),
                    CanSubscribe = table.Column<bool>(type: "bit", nullable: false),
                    RegisteredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastLoginAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastAdminChangeAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastAdminChangeType = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserSessions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SessionId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RefreshToken = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RefreshTokenExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSessions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_RefreshToken",
                table: "UserSessions",
                column: "RefreshToken");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_SessionId",
                table: "UserSessions",
                column: "SessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "UserSessions");
        }
    }
}
