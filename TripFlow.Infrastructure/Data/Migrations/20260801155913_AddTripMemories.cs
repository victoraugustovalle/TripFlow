using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TripFlow.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTripMemories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TripMemories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TripId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParticipantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Highlight = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Rating = table.Column<int>(type: "integer", nullable: true),
                    PhotoUrl = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripMemories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TripMemories_TripParticipants_ParticipantId",
                        column: x => x.ParticipantId,
                        principalTable: "TripParticipants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TripMemories_Trips_TripId",
                        column: x => x.TripId,
                        principalTable: "Trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TripMemories_ParticipantId",
                table: "TripMemories",
                column: "ParticipantId");

            migrationBuilder.CreateIndex(
                name: "IX_TripMemories_TripId_ParticipantId",
                table: "TripMemories",
                columns: new[] { "TripId", "ParticipantId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TripMemories");
        }
    }
}
