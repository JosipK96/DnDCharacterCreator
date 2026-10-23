using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CharacterCreatorApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class LevelUp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The scaffold gave both jsonb columns defaultValue "", which Postgres rejects. With no
            // result kept, every level of a character saved before this keeps the fixed value.
            migrationBuilder.AddColumn<string>(
                name: "HitDieResults",
                table: "Characters",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "HitDieResults",
                table: "CharacterDrafts",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            migrationBuilder.AddColumn<List<string>>(
                name: "LevelUpChoices",
                table: "CharacterDrafts",
                type: "text[]",
                nullable: false,
                // No draft saved before this is a level-up
                defaultValueSql: "'{}'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HitDieResults",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "HitDieResults",
                table: "CharacterDrafts");

            migrationBuilder.DropColumn(
                name: "LevelUpChoices",
                table: "CharacterDrafts");
        }
    }
}
