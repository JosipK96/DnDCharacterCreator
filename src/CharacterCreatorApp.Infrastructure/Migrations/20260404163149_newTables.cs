using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CharacterCreatorApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class newTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CantripsKnownAtLevel1",
                table: "Spellcasting");

            migrationBuilder.DropColumn(
                name: "InitialSpellbookSize",
                table: "Spellcasting");

            migrationBuilder.DropColumn(
                name: "MinimumSpellcastingLevel",
                table: "Spellcasting");

            migrationBuilder.DropColumn(
                name: "SpellsKnownAtLevel1",
                table: "Spellcasting");

            migrationBuilder.CreateTable(
                name: "ClassSpellProgression",
                columns: table => new
                {
                    ClassId = table.Column<string>(type: "character varying(50)", nullable: false),
                    CharacterLevel = table.Column<int>(type: "integer", nullable: false),
                    SpellsKnown = table.Column<int>(type: "integer", nullable: false),
                    CantripsKnown = table.Column<int>(type: "integer", nullable: false),
                    MaxSpellLevel = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassSpellProgression", x => new { x.ClassId, x.CharacterLevel });
                    table.ForeignKey(
                        name: "FK_ClassSpellProgression_Classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubclassSpellProgression",
                columns: table => new
                {
                    SubclassId = table.Column<string>(type: "text", nullable: false),
                    CharacterLevel = table.Column<int>(type: "integer", nullable: false),
                    SpellsKnown = table.Column<int>(type: "integer", nullable: false),
                    CantripsKnown = table.Column<int>(type: "integer", nullable: false),
                    MaxSpellLevel = table.Column<int>(type: "integer", nullable: false),
                    SpellcastingAbility = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubclassSpellProgression", x => new { x.SubclassId, x.CharacterLevel });
                    table.ForeignKey(
                        name: "FK_SubclassSpellProgression_Subclasses_SubclassId",
                        column: x => x.SubclassId,
                        principalTable: "Subclasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClassSpellProgression");

            migrationBuilder.DropTable(
                name: "SubclassSpellProgression");

            migrationBuilder.AddColumn<int>(
                name: "CantripsKnownAtLevel1",
                table: "Spellcasting",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "InitialSpellbookSize",
                table: "Spellcasting",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MinimumSpellcastingLevel",
                table: "Spellcasting",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "SpellsKnownAtLevel1",
                table: "Spellcasting",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
