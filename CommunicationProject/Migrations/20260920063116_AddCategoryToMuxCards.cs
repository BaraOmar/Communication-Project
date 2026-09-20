using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunicationProject.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryToMuxCards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "MuxCards",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.Sql(
                """
        UPDATE muxCard
        SET muxCard.Category = cardType.Category
        FROM MuxCards AS muxCard
        INNER JOIN CardTypes AS cardType
            ON cardType.Id = muxCard.CardTypeId;
        """);

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "MuxCards",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "MuxCards");
        }
    }
}
