using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CharacterCreatorApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CharacterFeatSpells : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // A character made before this keeps no per-feat picks; its spells stay in SpellIds
            migrationBuilder.AddColumn<string>(
                name: "FeatSpells",
                table: "Characters",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FeatSpells",
                table: "Characters");
        }
    }
}
