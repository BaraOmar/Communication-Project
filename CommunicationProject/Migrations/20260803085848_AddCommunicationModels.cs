using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunicationProject.Migrations
{
    /// <inheritdoc />
    public partial class AddCommunicationModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LinkTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LinkTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CommunicationLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    LinkTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SiteFromId = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    SiteToId = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunicationLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommunicationLinks_LinkTypes_LinkTypeId",
                        column: x => x.LinkTypeId,
                        principalTable: "LinkTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommunicationLinks_Sites_SiteFromId",
                        column: x => x.SiteFromId,
                        principalTable: "Sites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommunicationLinks_Sites_SiteToId",
                        column: x => x.SiteToId,
                        principalTable: "Sites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Stms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Number = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LinkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConnectedStmId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Stms_CommunicationLinks_LinkId",
                        column: x => x.LinkId,
                        principalTable: "CommunicationLinks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Stms_Stms_ConnectedStmId",
                        column: x => x.ConnectedStmId,
                        principalTable: "Stms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "E1s",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    E1Number = table.Column<int>(type: "int", nullable: false),
                    StmId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConnectedE1Id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_E1s", x => x.Id);
                    table.ForeignKey(
                        name: "FK_E1s_E1s_ConnectedE1Id",
                        column: x => x.ConnectedE1Id,
                        principalTable: "E1s",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_E1s_Stms_StmId",
                        column: x => x.StmId,
                        principalTable: "Stms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Sites_Name",
                table: "Sites",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationLinks_LinkTypeId_SiteFromId_SiteToId",
                table: "CommunicationLinks",
                columns: new[] { "LinkTypeId", "SiteFromId", "SiteToId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationLinks_Name",
                table: "CommunicationLinks",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationLinks_SiteFromId",
                table: "CommunicationLinks",
                column: "SiteFromId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationLinks_SiteToId",
                table: "CommunicationLinks",
                column: "SiteToId");

            migrationBuilder.CreateIndex(
                name: "IX_E1s_ConnectedE1Id",
                table: "E1s",
                column: "ConnectedE1Id",
                unique: true,
                filter: "[ConnectedE1Id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_E1s_StmId_E1Number",
                table: "E1s",
                columns: new[] { "StmId", "E1Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LinkTypes_Name",
                table: "LinkTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stms_ConnectedStmId",
                table: "Stms",
                column: "ConnectedStmId",
                unique: true,
                filter: "[ConnectedStmId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Stms_LinkId_Number",
                table: "Stms",
                columns: new[] { "LinkId", "Number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "E1s");

            migrationBuilder.DropTable(
                name: "Stms");

            migrationBuilder.DropTable(
                name: "CommunicationLinks");

            migrationBuilder.DropTable(
                name: "LinkTypes");

            migrationBuilder.DropIndex(
                name: "IX_Sites_Name",
                table: "Sites");
        }
    }
}
