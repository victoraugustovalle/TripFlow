using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TripFlow.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSettlementRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SettlementRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TripId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromParticipantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToParticipantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SettlementRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SettlementRecords_TripParticipants_FromParticipantId",
                        column: x => x.FromParticipantId,
                        principalTable: "TripParticipants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SettlementRecords_TripParticipants_ToParticipantId",
                        column: x => x.ToParticipantId,
                        principalTable: "TripParticipants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SettlementRecords_Trips_TripId",
                        column: x => x.TripId,
                        principalTable: "Trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SettlementRecords_FromParticipantId",
                table: "SettlementRecords",
                column: "FromParticipantId");

            migrationBuilder.CreateIndex(
                name: "IX_SettlementRecords_ToParticipantId",
                table: "SettlementRecords",
                column: "ToParticipantId");

            migrationBuilder.CreateIndex(
                name: "IX_SettlementRecords_TripId_Status",
                table: "SettlementRecords",
                columns: new[] { "TripId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SettlementRecords");
        }
    }
}
