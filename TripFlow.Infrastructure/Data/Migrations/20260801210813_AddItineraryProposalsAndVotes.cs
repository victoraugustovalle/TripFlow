using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TripFlow.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddItineraryProposalsAndVotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "ItineraryItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ItineraryProposalOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ItineraryItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Location = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Latitude = table.Column<double>(type: "double precision", nullable: true),
                    Longitude = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItineraryProposalOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItineraryProposalOptions_ItineraryItems_ItineraryItemId",
                        column: x => x.ItineraryItemId,
                        principalTable: "ItineraryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItineraryVotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ItineraryItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    OptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParticipantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItineraryVotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItineraryVotes_ItineraryItems_ItineraryItemId",
                        column: x => x.ItineraryItemId,
                        principalTable: "ItineraryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItineraryVotes_ItineraryProposalOptions_OptionId",
                        column: x => x.OptionId,
                        principalTable: "ItineraryProposalOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItineraryVotes_TripParticipants_ParticipantId",
                        column: x => x.ParticipantId,
                        principalTable: "TripParticipants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ItineraryProposalOptions_ItineraryItemId",
                table: "ItineraryProposalOptions",
                column: "ItineraryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ItineraryVotes_ItineraryItemId_ParticipantId",
                table: "ItineraryVotes",
                columns: new[] { "ItineraryItemId", "ParticipantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItineraryVotes_OptionId",
                table: "ItineraryVotes",
                column: "OptionId");

            migrationBuilder.CreateIndex(
                name: "IX_ItineraryVotes_ParticipantId",
                table: "ItineraryVotes",
                column: "ParticipantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItineraryVotes");

            migrationBuilder.DropTable(
                name: "ItineraryProposalOptions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "ItineraryItems");
        }
    }
}
