using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TripFlow.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTripCoverImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CoverImageUrl",
                table: "Trips",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoverImageUrl",
                table: "Trips");
        }
    }
}
