using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CharacterCreatorApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCharacterDraftChoiceFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ""CharacterDrafts""
                    ADD COLUMN IF NOT EXISTS ""AbilityScoreImprovements"" jsonb NOT NULL DEFAULT '[]',
                    ADD COLUMN IF NOT EXISTS ""SelectedLanguageIds"" text[] NOT NULL DEFAULT '{}',
                    ADD COLUMN IF NOT EXISTS ""SelectedRaceSkillIds"" text[] NOT NULL DEFAULT '{}';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AbilityScoreImprovements",
                table: "CharacterDrafts");

            migrationBuilder.DropColumn(
                name: "SelectedLanguageIds",
                table: "CharacterDrafts");

            migrationBuilder.DropColumn(
                name: "SelectedRaceSkillIds",
                table: "CharacterDrafts");
        }
    }
}
