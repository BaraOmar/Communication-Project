using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunicationProject.Migrations
{
    /// <inheritdoc />
    public partial class AddMuxTypeCardConfigurations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MuxTypeCardConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MuxTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CardTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CardCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MuxTypeCardConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MuxTypeCardConfigurations_CardTypes_CardTypeId",
                        column: x => x.CardTypeId,
                        principalTable: "CardTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MuxTypeCardConfigurations_MuxTypes_MuxTypeId",
                        column: x => x.MuxTypeId,
                        principalTable: "MuxTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MuxTypeCardConfigurations_CardTypeId",
                table: "MuxTypeCardConfigurations",
                column: "CardTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_MuxTypeCardConfigurations_MuxTypeId_CardTypeId",
                table: "MuxTypeCardConfigurations",
                columns: new[] { "MuxTypeId", "CardTypeId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MuxTypeCardConfigurations");
        }
    }
}
