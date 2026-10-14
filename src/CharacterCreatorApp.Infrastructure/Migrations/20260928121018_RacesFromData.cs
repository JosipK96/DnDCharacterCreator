using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CharacterCreatorApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RacesFromData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LanguageChoiceCount",
                table: "Subraces",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Speed",
                table: "Subraces",
                type: "integer",
                nullable: true);

            // The races already there are the 2014 ones; the reference data sync replaces them
            migrationBuilder.AddColumn<string>(
                name: "Edition",
                table: "Races",
                type: "character varying(4)",
                maxLength: 4,
                nullable: false,
                defaultValue: "2014");

            migrationBuilder.AddColumn<int>(
                name: "LanguageChoiceCount",
                table: "Races",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SkillChoiceCount",
                table: "Races",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<List<string>>(
                name: "SkillChoiceOptions",
                table: "Races",
                type: "text[]",
                nullable: false,
                defaultValueSql: "'{}'");

            migrationBuilder.CreateTable(
                name: "RaceSpells",
                columns: table => new
                {
                    RaceId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SpellId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaceSpells", x => new { x.RaceId, x.SpellId });
                    table.ForeignKey(
                        name: "FK_RaceSpells_Races_RaceId",
                        column: x => x.RaceId,
                        principalTable: "Races",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RaceSpells_Spells_SpellId",
                        column: x => x.SpellId,
                        principalTable: "Spells",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubraceSpells",
                columns: table => new
                {
                    SubraceId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SpellId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubraceSpells", x => new { x.SubraceId, x.SpellId });
                    table.ForeignKey(
                        name: "FK_SubraceSpells_Spells_SpellId",
                        column: x => x.SpellId,
                        principalTable: "Spells",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SubraceSpells_Subraces_SubraceId",
                        column: x => x.SubraceId,
                        principalTable: "Subraces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Races_Edition",
                table: "Races",
                column: "Edition");

            migrationBuilder.CreateIndex(
                name: "IX_RaceSpells_SpellId",
                table: "RaceSpells",
                column: "SpellId");

            migrationBuilder.CreateIndex(
                name: "IX_SubraceSpells_SpellId",
                table: "SubraceSpells",
                column: "SpellId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RaceSpells");

            migrationBuilder.DropTable(
                name: "SubraceSpells");

            migrationBuilder.DropIndex(
                name: "IX_Races_Edition",
                table: "Races");

            migrationBuilder.DropColumn(
                name: "LanguageChoiceCount",
                table: "Subraces");

            migrationBuilder.DropColumn(
                name: "Speed",
                table: "Subraces");

            migrationBuilder.DropColumn(
                name: "Edition",
                table: "Races");

            migrationBuilder.DropColumn(
                name: "LanguageChoiceCount",
                table: "Races");

            migrationBuilder.DropColumn(
                name: "SkillChoiceCount",
                table: "Races");

            migrationBuilder.DropColumn(
                name: "SkillChoiceOptions",
                table: "Races");
        }
    }
}
