using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunicationProject.Migrations
{
    /// <inheritdoc />
    public partial class AddStmDirection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Stms_LinkId_Number",
                table: "Stms");

            migrationBuilder.AddColumn<bool>(
                name: "IsFromSide",
                table: "Stms",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Stms_LinkId_Number_IsFromSide",
                table: "Stms",
                columns: new[] { "LinkId", "Number", "IsFromSide" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Stms_LinkId_Number_IsFromSide",
                table: "Stms");

            migrationBuilder.DropColumn(
                name: "IsFromSide",
                table: "Stms");

            migrationBuilder.CreateIndex(
                name: "IX_Stms_LinkId_Number",
                table: "Stms",
                columns: new[] { "LinkId", "Number" },
                unique: true);
        }
    }
}
