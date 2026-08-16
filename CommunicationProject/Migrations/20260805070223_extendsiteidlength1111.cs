using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunicationProject.Migrations
{
    /// <inheritdoc />
    public partial class extendsiteidlength1111 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove the foreign keys before altering their columns.
            migrationBuilder.DropForeignKey(
                name: "FK_CommunicationLinks_Sites_SiteFromId",
                table: "CommunicationLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_CommunicationLinks_Sites_SiteToId",
                table: "CommunicationLinks");

            // Remove the primary key before altering Sites.Id.
            migrationBuilder.DropPrimaryKey(
                name: "PK_Sites",
                table: "Sites");

            // Extend the foreign-key columns from 3 to 10 characters.
            migrationBuilder.AlterColumn<string>(
                name: "SiteFromId",
                table: "CommunicationLinks",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(3)",
                oldMaxLength: 3);

            migrationBuilder.AlterColumn<string>(
                name: "SiteToId",
                table: "CommunicationLinks",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(3)",
                oldMaxLength: 3);

            // Extend the primary-key column from 3 to 10 characters.
            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "Sites",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(3)",
                oldMaxLength: 3);

            // Restore the primary key.
            migrationBuilder.AddPrimaryKey(
                name: "PK_Sites",
                table: "Sites",
                column: "Id");

            // Restore the foreign keys.
            migrationBuilder.AddForeignKey(
                name: "FK_CommunicationLinks_Sites_SiteFromId",
                table: "CommunicationLinks",
                column: "SiteFromId",
                principalTable: "Sites",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CommunicationLinks_Sites_SiteToId",
                table: "CommunicationLinks",
                column: "SiteToId",
                principalTable: "Sites",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CommunicationLinks_Sites_SiteFromId",
                table: "CommunicationLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_CommunicationLinks_Sites_SiteToId",
                table: "CommunicationLinks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Sites",
                table: "Sites");

            // Return the foreign-key columns to 3 characters.
            migrationBuilder.AlterColumn<string>(
                name: "SiteFromId",
                table: "CommunicationLinks",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<string>(
                name: "SiteToId",
                table: "CommunicationLinks",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10);

            // Return Sites.Id to 3 characters.
            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "Sites",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Sites",
                table: "Sites",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CommunicationLinks_Sites_SiteFromId",
                table: "CommunicationLinks",
                column: "SiteFromId",
                principalTable: "Sites",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CommunicationLinks_Sites_SiteToId",
                table: "CommunicationLinks",
                column: "SiteToId",
                principalTable: "Sites",
                principalColumn: "Id");
        }
    }
}