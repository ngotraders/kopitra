using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kopitra.Api.Domain.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventFlowSnapshots",
                columns: table => new
                {
                    AggregateId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    AggregateName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    AggregateSequenceNumber = table.Column<int>(type: "int", nullable: false),
                    Data = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TimestampUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventFlowSnapshots", x => new { x.AggregateId, x.AggregateName });
                });

            migrationBuilder.CreateTable(
                name: "EventFlowEvents",
                columns: table => new
                {
                    GlobalSequenceNumber = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AggregateId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    AggregateName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    AggregateSequenceNumber = table.Column<int>(type: "int", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    EventVersion = table.Column<int>(type: "int", nullable: false),
                    Data = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TimestampUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventFlowEvents", x => x.GlobalSequenceNumber);
                    table.UniqueConstraint("AK_EventFlowEvents_AggregateId_AggregateSequenceNumber", x => new { x.AggregateId, x.AggregateSequenceNumber });
                });

            migrationBuilder.CreateTable(
                name: "ExpertAdvisorSessions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    AccountId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    State = table.Column<int>(type: "int", nullable: false),
                    JwtToken = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastHeartbeatAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpertAdvisorSessions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventFlowEvents_AggregateId",
                table: "EventFlowEvents",
                column: "AggregateId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpertAdvisorSessions_AccountId",
                table: "ExpertAdvisorSessions",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpertAdvisorSessions_ExpiresAt",
                table: "ExpertAdvisorSessions",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ExpertAdvisorSessions_State",
                table: "ExpertAdvisorSessions",
                column: "State");

            migrationBuilder.CreateIndex(
                name: "IX_ExpertAdvisorSessions_UserId",
                table: "ExpertAdvisorSessions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventFlowSnapshots");

            migrationBuilder.DropTable(
                name: "ExpertAdvisorSessions");

            migrationBuilder.DropTable(
                name: "EventFlowEvents");
        }
    }
}
