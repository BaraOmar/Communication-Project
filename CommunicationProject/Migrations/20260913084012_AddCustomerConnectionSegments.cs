using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunicationProject.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerConnectionSegments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomerConnectionSegments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerConnectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CommunicationPathSegmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    E1Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerConnectionSegments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerConnectionSegments_CommunicationPathSegments_CommunicationPathSegmentId",
                        column: x => x.CommunicationPathSegmentId,
                        principalTable: "CommunicationPathSegments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerConnectionSegments_CustomerConnections_CustomerConnectionId",
                        column: x => x.CustomerConnectionId,
                        principalTable: "CustomerConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomerConnectionSegments_E1s_E1Id",
                        column: x => x.E1Id,
                        principalTable: "E1s",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerConnectionSegments_CommunicationPathSegmentId",
                table: "CustomerConnectionSegments",
                column: "CommunicationPathSegmentId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerConnectionSegments_CustomerConnectionId_CommunicationPathSegmentId",
                table: "CustomerConnectionSegments",
                columns: new[] { "CustomerConnectionId", "CommunicationPathSegmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerConnectionSegments_E1Id",
                table: "CustomerConnectionSegments",
                column: "E1Id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerConnectionSegments");
        }
    }
}
