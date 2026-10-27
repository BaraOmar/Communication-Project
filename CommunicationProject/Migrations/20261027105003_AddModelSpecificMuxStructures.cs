using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CommunicationProject.Migrations
{
    /// <inheritdoc />
    public partial class AddModelSpecificMuxStructures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Muxes_MuxTypes_MuxTypeId",
                table: "Muxes");

            migrationBuilder.DropIndex(
                name: "IX_Muxes_SiteId",
                table: "Muxes");

            migrationBuilder.DropIndex(
                name: "IX_MuxCards_MuxId_SlotNumber",
                table: "MuxCards");

            migrationBuilder.DeleteData(
                table: "MuxTypes",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.DeleteData(
                table: "MuxTypes",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"));

            migrationBuilder.DropColumn(
                name: "LtuCardCount",
                table: "MuxTypes");

            migrationBuilder.DropColumn(
                name: "LtuPortsPerCard",
                table: "MuxTypes");

            migrationBuilder.DropColumn(
                name: "StmCardCount",
                table: "MuxTypes");

            migrationBuilder.DropColumn(
                name: "StmPortsPerCard",
                table: "MuxTypes");

            migrationBuilder.AddColumn<string>(
                name: "Model",
                table: "MuxTypes",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "PhysicalStmNumber",
                table: "MuxPorts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FirstPhysicalStmNumber",
                table: "Muxes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Model",
                table: "Muxes",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "SecondPhysicalStmNumber",
                table: "Muxes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsRequired",
                table: "MuxCards",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Section",
                table: "MuxCards",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ShelfNumber",
                table: "MuxCards",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "MuxTypeCards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MuxTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Section = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ShelfNumber = table.Column<int>(type: "int", nullable: false),
                    SlotNumber = table.Column<int>(type: "int", nullable: false),
                    PortCount = table.Column<int>(type: "int", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MuxTypeCards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MuxTypeCards_MuxTypes_MuxTypeId",
                        column: x => x.MuxTypeId,
                        principalTable: "MuxTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Muxes_SiteId_Name",
                table: "Muxes",
                columns: new[] { "SiteId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MuxCards_MuxId_Section_ShelfNumber_SlotNumber",
                table: "MuxCards",
                columns: new[] { "MuxId", "Section", "ShelfNumber", "SlotNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MuxTypeCards_MuxTypeId_Section_ShelfNumber_SlotNumber",
                table: "MuxTypeCards",
                columns: new[] { "MuxTypeId", "Section", "ShelfNumber", "SlotNumber" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Muxes_MuxTypes_MuxTypeId",
                table: "Muxes",
                column: "MuxTypeId",
                principalTable: "MuxTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Muxes_MuxTypes_MuxTypeId",
                table: "Muxes");

            migrationBuilder.DropTable(
                name: "MuxTypeCards");

            migrationBuilder.DropIndex(
                name: "IX_Muxes_SiteId_Name",
                table: "Muxes");

            migrationBuilder.DropIndex(
                name: "IX_MuxCards_MuxId_Section_ShelfNumber_SlotNumber",
                table: "MuxCards");

            migrationBuilder.DropColumn(
                name: "Model",
                table: "MuxTypes");

            migrationBuilder.DropColumn(
                name: "PhysicalStmNumber",
                table: "MuxPorts");

            migrationBuilder.DropColumn(
                name: "FirstPhysicalStmNumber",
                table: "Muxes");

            migrationBuilder.DropColumn(
                name: "Model",
                table: "Muxes");

            migrationBuilder.DropColumn(
                name: "SecondPhysicalStmNumber",
                table: "Muxes");

            migrationBuilder.DropColumn(
                name: "IsRequired",
                table: "MuxCards");

            migrationBuilder.DropColumn(
                name: "Section",
                table: "MuxCards");

            migrationBuilder.DropColumn(
                name: "ShelfNumber",
                table: "MuxCards");

            migrationBuilder.AddColumn<int>(
                name: "LtuCardCount",
                table: "MuxTypes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LtuPortsPerCard",
                table: "MuxTypes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StmCardCount",
                table: "MuxTypes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StmPortsPerCard",
                table: "MuxTypes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.InsertData(
                table: "MuxTypes",
                columns: new[] { "Id", "LtuCardCount", "LtuPortsPerCard", "Name", "StmCardCount", "StmPortsPerCard" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), 0, 0, "Ericsson", 0, 0 },
                    { new Guid("22222222-2222-2222-2222-222222222222"), 0, 0, "OSP", 0, 0 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Muxes_SiteId",
                table: "Muxes",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_MuxCards_MuxId_SlotNumber",
                table: "MuxCards",
                columns: new[] { "MuxId", "SlotNumber" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Muxes_MuxTypes_MuxTypeId",
                table: "Muxes",
                column: "MuxTypeId",
                principalTable: "MuxTypes",
                principalColumn: "Id");
        }
    }
}
