using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CharacterCreatorApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BackgroundFeatAlternativeCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FeatAlternativeCategory",
                table: "Backgrounds",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FeatAlternativeCategory",
                table: "Backgrounds");
        }
    }
}
