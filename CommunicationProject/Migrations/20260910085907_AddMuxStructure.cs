using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunicationProject.Migrations
{
    /// <inheritdoc />
    public partial class AddMuxStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CardTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    PortCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Muxes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SiteId = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    CommunicationLinkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Muxes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Muxes_CommunicationLinks_CommunicationLinkId",
                        column: x => x.CommunicationLinkId,
                        principalTable: "CommunicationLinks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Muxes_Sites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Sites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MuxCards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MuxId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CardTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SlotNumber = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MuxCards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MuxCards_CardTypes_CardTypeId",
                        column: x => x.CardTypeId,
                        principalTable: "CardTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MuxCards_Muxes_MuxId",
                        column: x => x.MuxId,
                        principalTable: "Muxes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MuxPorts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MuxCardId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PortNumber = table.Column<int>(type: "int", nullable: false),
                    E1Id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false, defaultValue: "Available")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MuxPorts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MuxPorts_E1s_E1Id",
                        column: x => x.E1Id,
                        principalTable: "E1s",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MuxPorts_MuxCards_MuxCardId",
                        column: x => x.MuxCardId,
                        principalTable: "MuxCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CardTypes_Name",
                table: "CardTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MuxCards_CardTypeId",
                table: "MuxCards",
                column: "CardTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_MuxCards_MuxId_SlotNumber",
                table: "MuxCards",
                columns: new[] { "MuxId", "SlotNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Muxes_CommunicationLinkId_SiteId_Name",
                table: "Muxes",
                columns: new[] { "CommunicationLinkId", "SiteId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Muxes_SiteId",
                table: "Muxes",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_MuxPorts_E1Id",
                table: "MuxPorts",
                column: "E1Id",
                unique: true,
                filter: "[E1Id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MuxPorts_MuxCardId_PortNumber",
                table: "MuxPorts",
                columns: new[] { "MuxCardId", "PortNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MuxPorts");

            migrationBuilder.DropTable(
                name: "MuxCards");

            migrationBuilder.DropTable(
                name: "CardTypes");

            migrationBuilder.DropTable(
                name: "Muxes");
        }
    }
}
