using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunicationProject.Migrations
{
    /// <inheritdoc />
    public partial class AddMuxCapacity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CardSlotCount",
                table: "Muxes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ShelfCount",
                table: "Muxes",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CardSlotCount",
                table: "Muxes");

            migrationBuilder.DropColumn(
                name: "ShelfCount",
                table: "Muxes");
        }
    }
}
