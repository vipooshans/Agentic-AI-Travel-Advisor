using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelAdvisor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddItineraryPlanFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DestinationId",
                table: "Itineraries",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedCost",
                table: "Itineraries",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Summary",
                table: "Itineraries",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Itineraries_DestinationId",
                table: "Itineraries",
                column: "DestinationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Itineraries_Destinations_DestinationId",
                table: "Itineraries",
                column: "DestinationId",
                principalTable: "Destinations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Itineraries_Destinations_DestinationId",
                table: "Itineraries");

            migrationBuilder.DropIndex(
                name: "IX_Itineraries_DestinationId",
                table: "Itineraries");

            migrationBuilder.DropColumn(
                name: "DestinationId",
                table: "Itineraries");

            migrationBuilder.DropColumn(
                name: "EstimatedCost",
                table: "Itineraries");

            migrationBuilder.DropColumn(
                name: "Summary",
                table: "Itineraries");
        }
    }
}
