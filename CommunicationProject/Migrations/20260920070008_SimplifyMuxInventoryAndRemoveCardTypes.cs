using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunicationProject.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyMuxInventoryAndRemoveCardTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MuxCards_CardTypes_CardTypeId",
                table: "MuxCards");

            migrationBuilder.DropTable(
                name: "CardTypes");

            migrationBuilder.DropIndex(
                name: "IX_MuxCards_CardTypeId",
                table: "MuxCards");

            migrationBuilder.DropIndex(
                name: "IX_MuxCards_MuxId_ShelfNumber_SlotNumber",
                table: "MuxCards");

            migrationBuilder.DropIndex(
                name: "IX_MuxCards_MuxId_SlotNumber",
                table: "MuxCards");
            migrationBuilder.Sql(
    """
    WITH RenumberedCards AS
    (
        SELECT
            Id,
            ROW_NUMBER() OVER
            (
                PARTITION BY MuxId
                ORDER BY
                    CASE
                        WHEN ShelfNumber IS NULL THEN 0
                        ELSE ShelfNumber
                    END,
                    SlotNumber,
                    Id
            ) AS NewSlotNumber
        FROM MuxCards
    )
    UPDATE muxCard
    SET SlotNumber = renumbered.NewSlotNumber
    FROM MuxCards AS muxCard
    INNER JOIN RenumberedCards AS renumbered
        ON renumbered.Id = muxCard.Id;
    """);
            migrationBuilder.DropColumn(
                name: "HasShelves",
                table: "MuxTypes");

            migrationBuilder.DropColumn(
                name: "CardSlotCount",
                table: "Muxes");

            migrationBuilder.DropColumn(
                name: "ShelfCount",
                table: "Muxes");

            migrationBuilder.DropColumn(
                name: "CardTypeId",
                table: "MuxCards");

            migrationBuilder.DropColumn(
                name: "ShelfNumber",
                table: "MuxCards");

            migrationBuilder.CreateIndex(
                name: "IX_MuxCards_MuxId_SlotNumber",
                table: "MuxCards",
                columns: new[] { "MuxId", "SlotNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MuxCards_MuxId_SlotNumber",
                table: "MuxCards");

            migrationBuilder.AddColumn<bool>(
                name: "HasShelves",
                table: "MuxTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

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

            migrationBuilder.AddColumn<Guid>(
                name: "CardTypeId",
                table: "MuxCards",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "ShelfNumber",
                table: "MuxCards",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CardTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PortCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardTypes", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "MuxTypes",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "HasShelves",
                value: true);

            migrationBuilder.UpdateData(
                table: "MuxTypes",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "HasShelves",
                value: false);

            migrationBuilder.CreateIndex(
                name: "IX_MuxCards_CardTypeId",
                table: "MuxCards",
                column: "CardTypeId");

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
                name: "IX_CardTypes_Name",
                table: "CardTypes",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MuxCards_CardTypes_CardTypeId",
                table: "MuxCards",
                column: "CardTypeId",
                principalTable: "CardTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
