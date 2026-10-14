using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CharacterCreatorApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSpellcastingTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Spellcasting",
                columns: table => new
                {
                    ClassId = table.Column<string>(type: "character varying(50)", nullable: false),
                    SpellcastingAbility = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsPreparedCaster = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CantripsKnownAtLevel1 = table.Column<int>(type: "integer", nullable: false),
                    SpellsKnownAtLevel1 = table.Column<int>(type: "integer", nullable: false),
                    InitialSpellbookSize = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Spellcasting", x => x.ClassId);
                    table.ForeignKey(
                        name: "FK_Spellcasting_Classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Spellcasting");
        }
    }
}
