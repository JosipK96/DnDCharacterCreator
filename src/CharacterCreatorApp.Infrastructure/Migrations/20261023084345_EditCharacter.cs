using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CharacterCreatorApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EditCharacter : Migration
    {
        /// <summary>
        /// Links each saved character to the completed draft that made it, which finalize kept but
        /// never pointed at the character: the same user, name, level, edition, race, subrace,
        /// class, subclass and background, and of several such the one saved closest to the
        /// character (finalize saves both at once). A draft goes to one character at most; a
        /// character left without one is edited from what it stores itself. Public for the test
        /// that runs it over rows the migration would meet.
        /// </summary>
        public const string LinkCharactersToTheirDraftsSql = """
            UPDATE "CharacterDrafts" AS d
            SET "CharacterId" = linked.character_id
            FROM (
                SELECT DISTINCT ON (closest.draft_id) closest.draft_id, closest.character_id
                FROM (
                    SELECT DISTINCT ON (c."Id")
                        c."Id" AS character_id,
                        candidate."Id" AS draft_id,
                        abs(extract(epoch FROM (COALESCE(candidate."UpdatedAt", candidate."CreatedAt") - c."CreatedAt"))) AS gap
                    FROM "Characters" AS c
                    JOIN "CharacterDrafts" AS candidate
                        ON candidate."IsCompleted"
                        AND candidate."CharacterId" IS NULL
                        AND candidate."CreatedByUserId" = c."UserId"
                        AND candidate."Name" = c."Name"
                        AND candidate."Level" = c."Level"
                        AND candidate."Edition" = c."Edition"
                        AND candidate."RaceId" = c."RaceId"
                        AND candidate."SubraceId" IS NOT DISTINCT FROM c."SubraceId"
                        AND candidate."ClassId" = c."ClassId"
                        AND candidate."SubclassId" IS NOT DISTINCT FROM c."SubclassId"
                        AND candidate."BackgroundId" = c."BackgroundId"
                    ORDER BY c."Id", gap
                ) AS closest
                ORDER BY closest.draft_id, closest.gap
            ) AS linked
            WHERE d."Id" = linked.draft_id;
            """;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CharacterId",
                table: "CharacterDrafts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<List<string>>(
                name: "ChoicesNeeded",
                table: "CharacterDrafts",
                type: "text[]",
                nullable: false,
                // Only an edit marks steps; no draft saved before this is one
                defaultValueSql: "'{}'");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterDrafts_CharacterId",
                table: "CharacterDrafts",
                column: "CharacterId");

            migrationBuilder.AddForeignKey(
                name: "FK_CharacterDrafts_Characters_CharacterId",
                table: "CharacterDrafts",
                column: "CharacterId",
                principalTable: "Characters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.Sql(LinkCharactersToTheirDraftsSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Only the links and the marks go, and the unsaved edits, which would otherwise come
            // back as new characters to resume; every character and its build stays
            migrationBuilder.Sql("""DELETE FROM "CharacterDrafts" WHERE "CharacterId" IS NOT NULL AND NOT "IsCompleted";""");

            migrationBuilder.DropForeignKey(
                name: "FK_CharacterDrafts_Characters_CharacterId",
                table: "CharacterDrafts");

            migrationBuilder.DropIndex(
                name: "IX_CharacterDrafts_CharacterId",
                table: "CharacterDrafts");

            migrationBuilder.DropColumn(
                name: "CharacterId",
                table: "CharacterDrafts");

            migrationBuilder.DropColumn(
                name: "ChoicesNeeded",
                table: "CharacterDrafts");
        }
    }
}
