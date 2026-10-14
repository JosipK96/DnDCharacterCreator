using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CharacterCreatorApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ToolChoices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ToolChoices",
                table: "Races",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "ToolChoices",
                table: "Classes",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "ToolPicks",
                table: "CharacterDrafts",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "ToolChoices",
                table: "Backgrounds",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ToolChoices",
                table: "Races");

            migrationBuilder.DropColumn(
                name: "ToolChoices",
                table: "Classes");

            migrationBuilder.DropColumn(
                name: "ToolPicks",
                table: "CharacterDrafts");

            migrationBuilder.DropColumn(
                name: "ToolChoices",
                table: "Backgrounds");
        }
    }
}
