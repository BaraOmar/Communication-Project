using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunicationProject.Migrations
{
    /// <inheritdoc />
    public partial class AllowMultipleCommunicationLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CommunicationLinks_LinkTypeId_SiteFromId_SiteToId",
                table: "CommunicationLinks");

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationLinks_LinkTypeId_SiteFromId_SiteToId_Name",
                table: "CommunicationLinks",
                columns: new[] { "LinkTypeId", "SiteFromId", "SiteToId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CommunicationLinks_LinkTypeId_SiteFromId_SiteToId_Name",
                table: "CommunicationLinks");

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationLinks_LinkTypeId_SiteFromId_SiteToId",
                table: "CommunicationLinks",
                columns: new[] { "LinkTypeId", "SiteFromId", "SiteToId" },
                unique: true);
        }
    }
}
