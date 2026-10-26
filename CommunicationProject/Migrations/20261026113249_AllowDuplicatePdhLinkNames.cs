using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunicationProject.Migrations
{
    /// <inheritdoc />
    public partial class AllowDuplicatePdhLinkNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CommunicationLinks_LinkTypeId_SiteFromId_SiteToId_Name",
                table: "CommunicationLinks");

            migrationBuilder.DropIndex(
                name: "IX_CommunicationLinks_Name_IsPrimary",
                table: "CommunicationLinks");

            migrationBuilder.AddColumn<bool>(
                name: "AllowDuplicateName",
                table: "CommunicationLinks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("""
    UPDATE link
    SET link.[AllowDuplicateName] = 1
    FROM [CommunicationLinks] AS link
    INNER JOIN [LinkTypes] AS linkType
        ON linkType.[Id] = link.[LinkTypeId]
    WHERE UPPER(LTRIM(RTRIM(linkType.[Name]))) = N'PDH';
    """);

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationLinks_LinkTypeId_SiteFromId_SiteToId_Name",
                table: "CommunicationLinks",
                columns: new[] { "LinkTypeId", "SiteFromId", "SiteToId", "Name" },
                unique: true,
                filter: "[AllowDuplicateName] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationLinks_Name_IsPrimary",
                table: "CommunicationLinks",
                columns: new[] { "Name", "IsPrimary" },
                unique: true,
                filter: "[AllowDuplicateName] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
    IF EXISTS (
        SELECT 1
        FROM [CommunicationLinks]
        GROUP BY [Name], [IsPrimary]
        HAVING COUNT(*) > 1
    )
    BEGIN
        THROW 51001,
            'Cannot roll back while duplicate link names exist. Give each link pair a unique name first.',
            1;
    END;
    """);

            migrationBuilder.DropIndex(
                name: "IX_CommunicationLinks_LinkTypeId_SiteFromId_SiteToId_Name",
                table: "CommunicationLinks");

            migrationBuilder.DropIndex(
                name: "IX_CommunicationLinks_Name_IsPrimary",
                table: "CommunicationLinks");

            migrationBuilder.DropColumn(
                name: "AllowDuplicateName",
                table: "CommunicationLinks");

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationLinks_LinkTypeId_SiteFromId_SiteToId_Name",
                table: "CommunicationLinks",
                columns: new[] { "LinkTypeId", "SiteFromId", "SiteToId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationLinks_Name_IsPrimary",
                table: "CommunicationLinks",
                columns: new[] { "Name", "IsPrimary" },
                unique: true);
        }
    }
}
