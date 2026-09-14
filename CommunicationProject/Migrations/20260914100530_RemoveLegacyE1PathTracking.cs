using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunicationProject.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLegacyE1PathTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
    IF EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = 'IX_E1s_PathId_NotNull'
          AND object_id = OBJECT_ID('dbo.E1s')
    )
    DROP INDEX IX_E1s_PathId_NotNull
    ON dbo.E1s;
    """);
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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

            migrationBuilder.Sql("""
    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = 'IX_E1s_PathId_NotNull'
          AND object_id = OBJECT_ID('dbo.E1s')
    )
    CREATE INDEX IX_E1s_PathId_NotNull
    ON dbo.E1s (PathId)
    WHERE PathId IS NOT NULL;
    """);
        }
    }
}
