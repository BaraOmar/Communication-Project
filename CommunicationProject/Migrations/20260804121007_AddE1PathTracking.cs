using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunicationProject.Migrations
{
    /// <inheritdoc />
    public partial class AddE1PathTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PathId",
                table: "E1s",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PathOrder",
                table: "E1s",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_E1s_PathId_PathOrder",
                table: "E1s",
                columns: new[] { "PathId", "PathOrder" },
                unique: true,
                filter: "[PathId] IS NOT NULL AND [PathOrder] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_E1s_PathId_PathOrder",
                table: "E1s");

            migrationBuilder.DropColumn(
                name: "PathId",
                table: "E1s");

            migrationBuilder.DropColumn(
                name: "PathOrder",
                table: "E1s");
        }
    }
}
