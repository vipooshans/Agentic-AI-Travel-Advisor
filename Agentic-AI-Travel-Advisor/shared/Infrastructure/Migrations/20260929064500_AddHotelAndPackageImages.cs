using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelAdvisor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHotelAndPackageImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "TravelPackages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Hotels",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "TravelPackages");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Hotels");
        }
    }
}
