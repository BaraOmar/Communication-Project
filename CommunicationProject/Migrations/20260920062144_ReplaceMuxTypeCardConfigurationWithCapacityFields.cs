using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunicationProject.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceMuxTypeCardConfigurationWithCapacityFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MuxTypeCardConfigurations");

            migrationBuilder.AddColumn<int>(
                name: "E1CardCount",
                table: "MuxTypes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "E1PortsPerCard",
                table: "MuxTypes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PowerCardCount",
                table: "MuxTypes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PowerPortsPerCard",
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

            migrationBuilder.UpdateData(
                table: "MuxTypes",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "E1CardCount", "E1PortsPerCard", "PowerCardCount", "PowerPortsPerCard", "StmCardCount", "StmPortsPerCard" },
                values: new object[] { 0, 0, 0, 0, 0, 0 });

            migrationBuilder.UpdateData(
                table: "MuxTypes",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "E1CardCount", "E1PortsPerCard", "PowerCardCount", "PowerPortsPerCard", "StmCardCount", "StmPortsPerCard" },
                values: new object[] { 0, 0, 0, 0, 0, 0 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "E1CardCount",
                table: "MuxTypes");

            migrationBuilder.DropColumn(
                name: "E1PortsPerCard",
                table: "MuxTypes");

            migrationBuilder.DropColumn(
                name: "PowerCardCount",
                table: "MuxTypes");

            migrationBuilder.DropColumn(
                name: "PowerPortsPerCard",
                table: "MuxTypes");

            migrationBuilder.DropColumn(
                name: "StmCardCount",
                table: "MuxTypes");

            migrationBuilder.DropColumn(
                name: "StmPortsPerCard",
                table: "MuxTypes");

            migrationBuilder.CreateTable(
                name: "MuxTypeCardConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CardTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MuxTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
    }
}
