using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunicationProject.Migrations
{
    /// <inheritdoc />
    public partial class UseLtuAndStmCards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Stop if a Power port has an assignment.
            migrationBuilder.Sql("""
        IF EXISTS (
            SELECT 1
            FROM [MuxPorts] AS p
            INNER JOIN [MuxCards] AS c
                ON c.[Id] = p.[MuxCardId]
            WHERE c.[Category] = N'Power'
              AND (
                  p.[E1Id] IS NOT NULL
                  OR p.[StmId] IS NOT NULL
              )
        )
        BEGIN
            THROW 51000,
                'A Power card has assigned resources. Release or move them before applying this migration.',
                1;
        END;

        DELETE p
        FROM [MuxPorts] AS p
        INNER JOIN [MuxCards] AS c
            ON c.[Id] = p.[MuxCardId]
        WHERE c.[Category] = N'Power';

        DELETE FROM [MuxCards]
        WHERE [Category] = N'Power';

        UPDATE [MuxCards]
        SET [Category] = N'LTU'
        WHERE [Category] = N'E1';
        """);

            // Existing E1 capacity becomes LTU capacity.
            migrationBuilder.RenameColumn(
                name: "E1CardCount",
                table: "MuxTypes",
                newName: "LtuCardCount");

            migrationBuilder.RenameColumn(
                name: "E1PortsPerCard",
                table: "MuxTypes",
                newName: "LtuPortsPerCard");

            migrationBuilder.DropColumn(
                name: "PowerCardCount",
                table: "MuxTypes");

            migrationBuilder.DropColumn(
                name: "PowerPortsPerCard",
                table: "MuxTypes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException(
                "This migration removes obsolete Power cards and their ports. " +
                "Restore a database backup to recover the original inventory.");
        }
    }
}
