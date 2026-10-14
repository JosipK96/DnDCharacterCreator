using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CharacterCreatorApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReferenceDataSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.CreateTable(
                name: "ReferenceDataVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AppliedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferenceDataVersions", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_Characters_Backgrounds_BackgroundId",
                table: "Characters",
                column: "BackgroundId",
                principalTable: "Backgrounds",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Characters_Classes_ClassId",
                table: "Characters",
                column: "ClassId",
                principalTable: "Classes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Characters_Races_RaceId",
                table: "Characters",
                column: "RaceId",
                principalTable: "Races",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Characters_Subclasses_SubclassId",
                table: "Characters",
                column: "SubclassId",
                principalTable: "Subclasses",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Characters_Subraces_SubraceId",
                table: "Characters",
                column: "SubraceId",
                principalTable: "Subraces",
                principalColumn: "Id");

            // EF has no setting for this. Checked immediately unless a transaction defers them, so
            // saving a character with an unknown id still fails on the spot; SeedService defers
            // them while it replaces the reference data. A migration that drops and re-adds one
            // of these keys has to repeat this.
            migrationBuilder.Sql("""
                ALTER TABLE "Characters" ALTER CONSTRAINT "FK_Characters_Backgrounds_BackgroundId" DEFERRABLE INITIALLY IMMEDIATE;
                ALTER TABLE "Characters" ALTER CONSTRAINT "FK_Characters_Classes_ClassId" DEFERRABLE INITIALLY IMMEDIATE;
                ALTER TABLE "Characters" ALTER CONSTRAINT "FK_Characters_Races_RaceId" DEFERRABLE INITIALLY IMMEDIATE;
                ALTER TABLE "Characters" ALTER CONSTRAINT "FK_Characters_Subclasses_SubclassId" DEFERRABLE INITIALLY IMMEDIATE;
                ALTER TABLE "Characters" ALTER CONSTRAINT "FK_Characters_Subraces_SubraceId" DEFERRABLE INITIALLY IMMEDIATE;
                """);
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

            migrationBuilder.DropTable(
                name: "ReferenceDataVersions");

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
    }
}
