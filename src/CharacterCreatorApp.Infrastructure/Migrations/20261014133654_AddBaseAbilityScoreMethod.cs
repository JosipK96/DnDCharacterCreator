using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CharacterCreatorApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBaseAbilityScoreMethod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BaseAbilityScoreMethod",
                table: "CharacterDrafts",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BaseAbilityScoreMethod",
                table: "CharacterDrafts");
        }
    }
}
