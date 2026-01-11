using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kopitra.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventEntity",
                columns: table => new
                {
                    GlobalSequenceNumber = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AggregateId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AggregateName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AggregateSequenceNumber = table.Column<int>(type: "int", nullable: false),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Data = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventEntity", x => x.GlobalSequenceNumber);
                });

            migrationBuilder.CreateTable(
                name: "SnapshotEntity",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AggregateId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AggregateName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AggregateSequenceNumber = table.Column<int>(type: "int", nullable: false),
                    Data = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SnapshotEntity", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventEntity_AggregateId_AggregateSequenceNumber",
                table: "EventEntity",
                columns: new[] { "AggregateId", "AggregateSequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SnapshotEntity_AggregateName_AggregateId_AggregateSequenceNumber",
                table: "SnapshotEntity",
                columns: new[] { "AggregateName", "AggregateId", "AggregateSequenceNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventEntity");

            migrationBuilder.DropTable(
                name: "SnapshotEntity");
        }
    }
}
