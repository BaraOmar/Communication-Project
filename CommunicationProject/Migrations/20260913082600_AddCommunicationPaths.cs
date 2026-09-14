using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunicationProject.Migrations
{
    /// <inheritdoc />
    public partial class AddCommunicationPaths : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CommunicationPaths",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunicationPaths", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CommunicationPathSegments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CommunicationPathId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CommunicationLinkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunicationPathSegments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommunicationPathSegments_CommunicationLinks_CommunicationLinkId",
                        column: x => x.CommunicationLinkId,
                        principalTable: "CommunicationLinks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommunicationPathSegments_CommunicationPaths_CommunicationPathId",
                        column: x => x.CommunicationPathId,
                        principalTable: "CommunicationPaths",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationPaths_Name",
                table: "CommunicationPaths",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationPathSegments_CommunicationLinkId",
                table: "CommunicationPathSegments",
                column: "CommunicationLinkId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationPathSegments_CommunicationPathId_Order",
                table: "CommunicationPathSegments",
                columns: new[] { "CommunicationPathId", "Order" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommunicationPathSegments");

            migrationBuilder.DropTable(
                name: "CommunicationPaths");
        }
    }
}
