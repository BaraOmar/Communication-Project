using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunicationProject.Migrations
{
    /// <inheritdoc />
    public partial class AddE1CrossConnectionState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CrossConnectionState",
                table: "E1s",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Available");

            migrationBuilder.AddCheckConstraint(
                name: "CK_E1s_CrossConnectionState",
                table: "E1s",
                sql: "[CrossConnectionState] IN ('Available', 'CrossConnected', 'ExtendExistingPath')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_E1s_CrossConnectionState",
                table: "E1s");

            migrationBuilder.DropColumn(
                name: "CrossConnectionState",
                table: "E1s");
        }
    }
}
