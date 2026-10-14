using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CharacterCreatorApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFightingStyleIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<string>>(
                name: "FightingStyleIds",
                table: "CharacterDrafts",
                type: "text[]",
                nullable: false,
                // Drafts saved before this have picked none
                defaultValueSql: "'{}'");

            RenameFeatIds(migrationBuilder, toEditionId: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            RenameFeatIds(migrationBuilder, toEditionId: false);

            migrationBuilder.DropColumn(
                name: "FightingStyleIds",
                table: "CharacterDrafts");
        }

        // The 2024 styles took an edition suffix when the 2014 ones arrived under the same names
        // (archery became archery_2024), so a feat taken for an improvement keeps its id
        private static void RenameFeatIds(MigrationBuilder migrationBuilder, bool toEditionId)
        {
            string[] styles = ["archery", "defense", "dueling", "great_weapon_fighting", "protection", "two_weapon_fighting"];

            foreach (var style in styles)
            {
                var (from, to) = toEditionId ? (style, style + "_2024") : (style + "_2024", style);

                migrationBuilder.Sql($"""
                    UPDATE "CharacterDrafts" SET "SelectedFeatIds" = array_replace("SelectedFeatIds", '{from}', '{to}');
                    UPDATE "Characters" SET "FeatIds" = (
                        SELECT jsonb_agg(CASE WHEN id = '{from}' THEN '{to}' ELSE id END ORDER BY n)
                        FROM jsonb_array_elements_text("FeatIds") WITH ORDINALITY AS f(id, n))
                    WHERE "FeatIds" ? '{from}';
                    """);
            }
        }
    }
}
