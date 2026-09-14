using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunicationProject.Migrations
{
    /// <inheritdoc />
    public partial class AddE1ConnectionGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ConnectionGroupId",
                table: "E1s",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_E1s_ConnectionGroupId",
                table: "E1s",
                column: "ConnectionGroupId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_E1s_ConnectionGroupId",
                table: "E1s");

            migrationBuilder.DropColumn(
                name: "ConnectionGroupId",
                table: "E1s");
        }
    }
}
