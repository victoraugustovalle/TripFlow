using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TripFlow.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProactiveNudgeLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProactiveNudgeLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TripId = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    FiredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProactiveNudgeLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProactiveNudgeLogs_Trips_TripId",
                        column: x => x.TripId,
                        principalTable: "Trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProactiveNudgeLogs_TripId_Key",
                table: "ProactiveNudgeLogs",
                columns: new[] { "TripId", "Key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProactiveNudgeLogs");
        }
    }
}
