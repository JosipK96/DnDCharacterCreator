using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CharacterCreatorApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSpellProgressionTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClassSpellProgression_Classes_ClassId",
                table: "ClassSpellProgression");

            migrationBuilder.DropForeignKey(
                name: "FK_SubclassSpellProgression_Subclasses_SubclassId",
                table: "SubclassSpellProgression");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SubclassSpellProgression",
                table: "SubclassSpellProgression");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ClassSpellProgression",
                table: "ClassSpellProgression");

            migrationBuilder.RenameTable(
                name: "SubclassSpellProgression",
                newName: "SubclassSpellProgressions");

            migrationBuilder.RenameTable(
                name: "ClassSpellProgression",
                newName: "ClassSpellProgressions");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SubclassSpellProgressions",
                table: "SubclassSpellProgressions",
                columns: new[] { "SubclassId", "CharacterLevel" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_ClassSpellProgressions",
                table: "ClassSpellProgressions",
                columns: new[] { "ClassId", "CharacterLevel" });

            migrationBuilder.AddForeignKey(
                name: "FK_ClassSpellProgressions_Classes_ClassId",
                table: "ClassSpellProgressions",
                column: "ClassId",
                principalTable: "Classes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SubclassSpellProgressions_Subclasses_SubclassId",
                table: "SubclassSpellProgressions",
                column: "SubclassId",
                principalTable: "Subclasses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClassSpellProgressions_Classes_ClassId",
                table: "ClassSpellProgressions");

            migrationBuilder.DropForeignKey(
                name: "FK_SubclassSpellProgressions_Subclasses_SubclassId",
                table: "SubclassSpellProgressions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SubclassSpellProgressions",
                table: "SubclassSpellProgressions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ClassSpellProgressions",
                table: "ClassSpellProgressions");

            migrationBuilder.RenameTable(
                name: "SubclassSpellProgressions",
                newName: "SubclassSpellProgression");

            migrationBuilder.RenameTable(
                name: "ClassSpellProgressions",
                newName: "ClassSpellProgression");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SubclassSpellProgression",
                table: "SubclassSpellProgression",
                columns: new[] { "SubclassId", "CharacterLevel" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_ClassSpellProgression",
                table: "ClassSpellProgression",
                columns: new[] { "ClassId", "CharacterLevel" });

            migrationBuilder.AddForeignKey(
                name: "FK_ClassSpellProgression_Classes_ClassId",
                table: "ClassSpellProgression",
                column: "ClassId",
                principalTable: "Classes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SubclassSpellProgression_Subclasses_SubclassId",
                table: "SubclassSpellProgression",
                column: "SubclassId",
                principalTable: "Subclasses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
