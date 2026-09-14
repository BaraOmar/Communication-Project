using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunicationProject.Migrations
{
    /// <inheritdoc />
    public partial class AddE1OperationalStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "E1s",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Available");

            migrationBuilder.AddColumn<DateTime>(
                name: "VisitDate",
                table: "E1s",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VisitorName",
                table: "E1s",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
            migrationBuilder.Sql("""
    UPDATE E1s
    SET Status = 'Connected'
    WHERE ConnectionGroupId IS NOT NULL;
    """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "E1s");

            migrationBuilder.DropColumn(
                name: "VisitDate",
                table: "E1s");

            migrationBuilder.DropColumn(
                name: "VisitorName",
                table: "E1s");
        }
    }
}
