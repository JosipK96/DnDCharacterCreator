using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CharacterCreatorApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCharacterModelWithNavigationProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Characters_BackgroundId",
                table: "Characters",
                column: "BackgroundId");

            migrationBuilder.CreateIndex(
                name: "IX_Characters_SubclassId",
                table: "Characters",
                column: "SubclassId");

            migrationBuilder.CreateIndex(
                name: "IX_Characters_SubraceId",
                table: "Characters",
                column: "SubraceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Characters_Backgrounds_BackgroundId",
                table: "Characters",
                column: "BackgroundId",
                principalTable: "Backgrounds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Characters_Classes_ClassId",
                table: "Characters",
                column: "ClassId",
                principalTable: "Classes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Characters_Races_RaceId",
                table: "Characters",
                column: "RaceId",
                principalTable: "Races",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Characters_Subclasses_SubclassId",
                table: "Characters",
                column: "SubclassId",
                principalTable: "Subclasses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Characters_Subraces_SubraceId",
                table: "Characters",
                column: "SubraceId",
                principalTable: "Subraces",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Characters_Backgrounds_BackgroundId",
                table: "Characters");

            migrationBuilder.DropForeignKey(
                name: "FK_Characters_Classes_ClassId",
                table: "Characters");

            migrationBuilder.DropForeignKey(
                name: "FK_Characters_Races_RaceId",
                table: "Characters");

            migrationBuilder.DropForeignKey(
                name: "FK_Characters_Subclasses_SubclassId",
                table: "Characters");

            migrationBuilder.DropForeignKey(
                name: "FK_Characters_Subraces_SubraceId",
                table: "Characters");

            migrationBuilder.DropIndex(
                name: "IX_Characters_BackgroundId",
                table: "Characters");

            migrationBuilder.DropIndex(
                name: "IX_Characters_SubclassId",
                table: "Characters");

            migrationBuilder.DropIndex(
                name: "IX_Characters_SubraceId",
                table: "Characters");
        }
    }
}
