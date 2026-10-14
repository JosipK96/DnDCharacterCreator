using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CharacterCreatorApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FeatAndRaceChoices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SpellChoice",
                table: "Subraces",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpellChoice",
                table: "Races",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpellChoice",
                table: "Feats",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FeatSavingThrowAbilityIds",
                table: "Characters",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "Size",
                table: "Characters",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BackgroundFeatId",
                table: "CharacterDrafts",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FeatSpells",
                table: "CharacterDrafts",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<List<string>>(
                name: "RaceSpellIds",
                table: "CharacterDrafts",
                type: "text[]",
                nullable: false,
                defaultValueSql: "'{}'");

            migrationBuilder.AddColumn<string>(
                name: "Size",
                table: "CharacterDrafts",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FeatChoiceCategory",
                table: "Backgrounds",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FeatSpellListClassId",
                table: "Backgrounds",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SpellChoice",
                table: "Subraces");

            migrationBuilder.DropColumn(
                name: "SpellChoice",
                table: "Races");

            migrationBuilder.DropColumn(
                name: "SpellChoice",
                table: "Feats");

            migrationBuilder.DropColumn(
                name: "FeatSavingThrowAbilityIds",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Size",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "BackgroundFeatId",
                table: "CharacterDrafts");

            migrationBuilder.DropColumn(
                name: "FeatSpells",
                table: "CharacterDrafts");

            migrationBuilder.DropColumn(
                name: "RaceSpellIds",
                table: "CharacterDrafts");

            migrationBuilder.DropColumn(
                name: "Size",
                table: "CharacterDrafts");

            migrationBuilder.DropColumn(
                name: "FeatChoiceCategory",
                table: "Backgrounds");

            migrationBuilder.DropColumn(
                name: "FeatSpellListClassId",
                table: "Backgrounds");
        }
    }
}
