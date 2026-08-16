using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunicationProject.Migrations
{
    /// <inheritdoc />
    public partial class ExtendSiteIdentifiersTo100 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove relationships that depend on Sites.Id.
            migrationBuilder.DropForeignKey(
                name: "FK_CommunicationLinks_Sites_SiteFromId",
                table: "CommunicationLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_CommunicationLinks_Sites_SiteToId",
                table: "CommunicationLinks");

            // Extend the foreign-key columns.
            migrationBuilder.AlterColumn<string>(
                name: "SiteFromId",
                table: "CommunicationLinks",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<string>(
                name: "SiteToId",
                table: "CommunicationLinks",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10);

            // Extend the Sites primary-key column.
            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "Sites",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10);

            // Recreate the relationships.
            migrationBuilder.AddForeignKey(
                name: "FK_CommunicationLinks_Sites_SiteFromId",
                table: "CommunicationLinks",
                column: "SiteFromId",
                principalTable: "Sites",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CommunicationLinks_Sites_SiteToId",
                table: "CommunicationLinks",
                column: "SiteToId",
                principalTable: "Sites",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CommunicationLinks_Sites_SiteFromId",
                table: "CommunicationLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_CommunicationLinks_Sites_SiteToId",
                table: "CommunicationLinks");

            migrationBuilder.AlterColumn<string>(
                name: "SiteFromId",
                table: "CommunicationLinks",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "SiteToId",
                table: "CommunicationLinks",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "Sites",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddForeignKey(
                name: "FK_CommunicationLinks_Sites_SiteFromId",
                table: "CommunicationLinks",
                column: "SiteFromId",
                principalTable: "Sites",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CommunicationLinks_Sites_SiteToId",
                table: "CommunicationLinks",
                column: "SiteToId",
                principalTable: "Sites",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
