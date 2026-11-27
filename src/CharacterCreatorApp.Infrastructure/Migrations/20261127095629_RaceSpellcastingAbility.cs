using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CharacterCreatorApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RaceSpellcastingAbility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SpellcastingAbility",
                table: "Subraces",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpellcastingAbility",
                table: "Races",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RaceSpellAbilityId",
                table: "Characters",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RaceSpellAbilityId",
                table: "CharacterDrafts",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SpellcastingAbility",
                table: "Subraces");

            migrationBuilder.DropColumn(
                name: "SpellcastingAbility",
                table: "Races");

            migrationBuilder.DropColumn(
                name: "RaceSpellAbilityId",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "RaceSpellAbilityId",
                table: "CharacterDrafts");
        }
    }
}
