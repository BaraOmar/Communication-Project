using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunicationProject.Migrations
{
    /// <inheritdoc />
    public partial class AddReverseLinksAndRemoveIsFromSide : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Stms_LinkId_Number_IsFromSide",
                table: "Stms");

            migrationBuilder.DropIndex(
                name: "IX_CommunicationLinks_Name",
                table: "CommunicationLinks");

            migrationBuilder.DropColumn(
                name: "IsFromSide",
                table: "Stms");

            migrationBuilder.AlterColumn<string>(
                name: "Number",
                table: "Stms",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "CommunicationLinks",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150);

            migrationBuilder.AddColumn<Guid>(
                name: "ConnectedLinkId",
                table: "CommunicationLinks",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPrimary",
                table: "CommunicationLinks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Stms_LinkId_Number",
                table: "Stms",
                columns: new[] { "LinkId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationLinks_ConnectedLinkId",
                table: "CommunicationLinks",
                column: "ConnectedLinkId",
                unique: true,
                filter: "[ConnectedLinkId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationLinks_Name_IsPrimary",
                table: "CommunicationLinks",
                columns: new[] { "Name", "IsPrimary" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CommunicationLinks_CommunicationLinks_ConnectedLinkId",
                table: "CommunicationLinks",
                column: "ConnectedLinkId",
                principalTable: "CommunicationLinks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CommunicationLinks_CommunicationLinks_ConnectedLinkId",
                table: "CommunicationLinks");

            migrationBuilder.DropIndex(
                name: "IX_Stms_LinkId_Number",
                table: "Stms");

            migrationBuilder.DropIndex(
                name: "IX_CommunicationLinks_ConnectedLinkId",
                table: "CommunicationLinks");

            migrationBuilder.DropIndex(
                name: "IX_CommunicationLinks_Name_IsPrimary",
                table: "CommunicationLinks");

            migrationBuilder.DropColumn(
                name: "ConnectedLinkId",
                table: "CommunicationLinks");

            migrationBuilder.DropColumn(
                name: "IsPrimary",
                table: "CommunicationLinks");

            migrationBuilder.AlterColumn<string>(
                name: "Number",
                table: "Stms",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<bool>(
                name: "IsFromSide",
                table: "Stms",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "CommunicationLinks",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.CreateIndex(
                name: "IX_Stms_LinkId_Number_IsFromSide",
                table: "Stms",
                columns: new[] { "LinkId", "Number", "IsFromSide" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationLinks_Name",
                table: "CommunicationLinks",
                column: "Name",
                unique: true);
        }
    }
}
