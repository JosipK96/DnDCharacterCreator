using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CharacterCreatorApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ClassRulesFromData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The defaults only carry the existing rows through the migration: the seed files
            // changed with it, so SeedService replaces every class on the next start. The arrays
            // had no default as scaffolded, which fails on a table that has rows.
            migrationBuilder.AddColumn<List<int>>(
                name: "AsiLevels",
                table: "Classes",
                type: "integer[]",
                nullable: false,
                defaultValueSql: "'{}'");

            migrationBuilder.AddColumn<string>(
                name: "Edition",
                table: "Classes",
                type: "character varying(4)",
                maxLength: 4,
                nullable: false,
                defaultValue: "2014");

            migrationBuilder.AddColumn<List<string>>(
                name: "SavingThrowAbilityIds",
                table: "Classes",
                type: "text[]",
                nullable: false,
                defaultValueSql: "'{}'");

            migrationBuilder.AddColumn<int>(
                name: "SubclassLevel",
                table: "Classes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Classes_Edition",
                table: "Classes",
                column: "Edition");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Classes_Edition",
                table: "Classes");

            migrationBuilder.DropColumn(
                name: "AsiLevels",
                table: "Classes");

            migrationBuilder.DropColumn(
                name: "Edition",
                table: "Classes");

            migrationBuilder.DropColumn(
                name: "SavingThrowAbilityIds",
                table: "Classes");

            migrationBuilder.DropColumn(
                name: "SubclassLevel",
                table: "Classes");
        }
    }
}
