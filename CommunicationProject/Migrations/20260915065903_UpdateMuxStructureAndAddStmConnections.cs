using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CommunicationProject.Migrations
{
    /// <inheritdoc />
    public partial class UpdateMuxStructureAndAddStmConnections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Muxes_CommunicationLinks_CommunicationLinkId",
                table: "Muxes");

            migrationBuilder.DropIndex(
                name: "IX_Muxes_CommunicationLinkId_SiteId_Name",
                table: "Muxes");

            migrationBuilder.DropIndex(
                name: "IX_MuxCards_MuxId_SlotNumber",
                table: "MuxCards");

            migrationBuilder.DropColumn(
                name: "CommunicationLinkId",
                table: "Muxes");

            migrationBuilder.AddColumn<Guid>(
                name: "StmId",
                table: "MuxPorts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MuxTypeId",
                table: "Muxes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ShelfNumber",
                table: "MuxCards",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MuxTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    HasShelves = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MuxTypes", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "MuxTypes",
                columns: new[] { "Id", "HasShelves", "Name" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), true, "Ericsson" },
                    { new Guid("22222222-2222-2222-2222-222222222222"), false, "OSP" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_MuxPorts_StmId",
                table: "MuxPorts",
                column: "StmId",
                unique: true,
                filter: "[StmId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Muxes_MuxTypeId",
                table: "Muxes",
                column: "MuxTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_MuxCards_MuxId_ShelfNumber_SlotNumber",
                table: "MuxCards",
                columns: new[] { "MuxId", "ShelfNumber", "SlotNumber" },
                unique: true,
                filter: "[ShelfNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MuxCards_MuxId_SlotNumber",
                table: "MuxCards",
                columns: new[] { "MuxId", "SlotNumber" },
                unique: true,
                filter: "[ShelfNumber] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MuxTypes_Name",
                table: "MuxTypes",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Muxes_MuxTypes_MuxTypeId",
                table: "Muxes",
                column: "MuxTypeId",
                principalTable: "MuxTypes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MuxPorts_Stms_StmId",
                table: "MuxPorts",
                column: "StmId",
                principalTable: "Stms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException(
                "This migration cannot be safely rolled back because the old MUX-to-CommunicationLink relationship data is removed.");
        }
    }
}
