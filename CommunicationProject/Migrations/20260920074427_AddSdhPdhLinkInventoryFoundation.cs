using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunicationProject.Migrations
{
    /// <inheritdoc />
    public partial class AddSdhPdhLinkInventoryFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_E1s_StmId_E1Number",
                table: "E1s");

            migrationBuilder.AddColumn<Guid>(
                name: "SdhLinkCardId",
                table: "Stms",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "StmId",
                table: "E1s",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<string>(
                name: "E1Number",
                table: "E1s",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(5)",
                oldMaxLength: 5);

            migrationBuilder.AlterColumn<string>(
                name: "ConnectionType",
                table: "E1s",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Unassigned",
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32,
                oldDefaultValue: "Physical");

            migrationBuilder.AddColumn<Guid>(
                name: "LinkId",
                table: "E1s",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql(
                """
    UPDATE e1
    SET e1.LinkId = stm.LinkId
    FROM E1s AS e1
    INNER JOIN Stms AS stm
        ON stm.Id = e1.StmId;
    """);

            migrationBuilder.AlterColumn<Guid>(
                name: "LinkId",
                table: "E1s",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "SdhLinkCards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LinkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Number = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SdhLinkCards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SdhLinkCards_CommunicationLinks_LinkId",
                        column: x => x.LinkId,
                        principalTable: "CommunicationLinks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Stms_SdhLinkCardId",
                table: "Stms",
                column: "SdhLinkCardId");

            migrationBuilder.CreateIndex(
                name: "IX_E1s_LinkId_E1Number",
                table: "E1s",
                columns: new[] { "LinkId", "E1Number" },
                unique: true,
                filter: "[StmId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_E1s_StmId_E1Number",
                table: "E1s",
                columns: new[] { "StmId", "E1Number" },
                unique: true,
                filter: "[StmId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SdhLinkCards_LinkId_Number",
                table: "SdhLinkCards",
                columns: new[] { "LinkId", "Number" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_E1s_CommunicationLinks_LinkId",
                table: "E1s",
                column: "LinkId",
                principalTable: "CommunicationLinks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Stms_SdhLinkCards_SdhLinkCardId",
                table: "Stms",
                column: "SdhLinkCardId",
                principalTable: "SdhLinkCards",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_E1s_CommunicationLinks_LinkId",
                table: "E1s");

            migrationBuilder.DropForeignKey(
                name: "FK_Stms_SdhLinkCards_SdhLinkCardId",
                table: "Stms");

            migrationBuilder.DropTable(
                name: "SdhLinkCards");

            migrationBuilder.DropIndex(
                name: "IX_Stms_SdhLinkCardId",
                table: "Stms");

            migrationBuilder.DropIndex(
                name: "IX_E1s_LinkId_E1Number",
                table: "E1s");

            migrationBuilder.DropIndex(
                name: "IX_E1s_StmId_E1Number",
                table: "E1s");

            migrationBuilder.DropColumn(
                name: "SdhLinkCardId",
                table: "Stms");

            migrationBuilder.DropColumn(
                name: "LinkId",
                table: "E1s");

            migrationBuilder.AlterColumn<Guid>(
                name: "StmId",
                table: "E1s",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "E1Number",
                table: "E1s",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "ConnectionType",
                table: "E1s",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Physical",
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32,
                oldDefaultValue: "Unassigned");

            migrationBuilder.CreateIndex(
                name: "IX_E1s_StmId_E1Number",
                table: "E1s",
                columns: new[] { "StmId", "E1Number" },
                unique: true);
        }
    }
}
