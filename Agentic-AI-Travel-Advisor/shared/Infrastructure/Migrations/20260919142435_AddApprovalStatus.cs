using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelAdvisor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddApprovalStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ApprovalStatus",
                table: "TravelPackages",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ApprovalStatus",
                table: "Hotels",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "TravelPackages");

            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "Hotels");
        }
    }
}
