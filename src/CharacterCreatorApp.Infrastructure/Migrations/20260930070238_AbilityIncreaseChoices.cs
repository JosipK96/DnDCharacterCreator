using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CharacterCreatorApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AbilityIncreaseChoices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AbilityChoiceCount",
                table: "Subraces",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<List<string>>(
                name: "AbilityChoiceOptions",
                table: "Subraces",
                type: "text[]",
                nullable: false,
                defaultValueSql: "'{}'");

            migrationBuilder.AddColumn<string>(
                name: "FeatChoiceCategory",
                table: "Subraces",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FeatChoiceCount",
                table: "Subraces",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SkillChoiceCount",
                table: "Subraces",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<List<string>>(
                name: "SkillChoiceOptions",
                table: "Subraces",
                type: "text[]",
                nullable: false,
                defaultValueSql: "'{}'");

            migrationBuilder.AddColumn<int>(
                name: "AbilityChoiceCount",
                table: "Races",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<List<string>>(
                name: "AbilityChoiceOptions",
                table: "Races",
                type: "text[]",
                nullable: false,
                defaultValueSql: "'{}'");

            migrationBuilder.AddColumn<string>(
                name: "FeatChoiceCategory",
                table: "Races",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FeatChoiceCount",
                table: "Races",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "AbilityIncrease",
                table: "Feats",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FeatAbilityIncreases",
                table: "CharacterDrafts",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            // One entry per feat already taken, so the list lines up with SelectedFeatIds; a feat
            // with a fixed increase needs no pick, and one with a choice asks for it at finalize
            migrationBuilder.Sql("""
                UPDATE "CharacterDrafts"
                SET "FeatAbilityIncreases" = (SELECT jsonb_agg('{}'::jsonb) FROM unnest("SelectedFeatIds"))
                WHERE cardinality("SelectedFeatIds") > 0;
                """);

            migrationBuilder.AddColumn<string>(
                name: "RaceAbilityIncreases",
                table: "CharacterDrafts",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AbilityChoiceCount",
                table: "Subraces");

            migrationBuilder.DropColumn(
                name: "AbilityChoiceOptions",
                table: "Subraces");

            migrationBuilder.DropColumn(
                name: "FeatChoiceCategory",
                table: "Subraces");

            migrationBuilder.DropColumn(
                name: "FeatChoiceCount",
                table: "Subraces");

            migrationBuilder.DropColumn(
                name: "SkillChoiceCount",
                table: "Subraces");

            migrationBuilder.DropColumn(
                name: "SkillChoiceOptions",
                table: "Subraces");

            migrationBuilder.DropColumn(
                name: "AbilityChoiceCount",
                table: "Races");

            migrationBuilder.DropColumn(
                name: "AbilityChoiceOptions",
                table: "Races");

            migrationBuilder.DropColumn(
                name: "FeatChoiceCategory",
                table: "Races");

            migrationBuilder.DropColumn(
                name: "FeatChoiceCount",
                table: "Races");

            migrationBuilder.DropColumn(
                name: "AbilityIncrease",
                table: "Feats");

            migrationBuilder.DropColumn(
                name: "FeatAbilityIncreases",
                table: "CharacterDrafts");

            migrationBuilder.DropColumn(
                name: "RaceAbilityIncreases",
                table: "CharacterDrafts");
        }
    }
}
