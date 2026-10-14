using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CharacterCreatorApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SubclassSpellSchools : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The empty-array defaults are added by hand: the columns had none as scaffolded,
            // which fails on a table that has rows. The seeder rewrites them from the files.
            migrationBuilder.AddColumn<List<int>>(
                name: "AnySchoolSpellLevels",
                table: "Subclasses",
                type: "integer[]",
                nullable: false,
                defaultValueSql: "'{}'");

            migrationBuilder.AddColumn<List<string>>(
                name: "SpellSchools",
                table: "Subclasses",
                type: "text[]",
                nullable: false,
                defaultValueSql: "'{}'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnySchoolSpellLevels",
                table: "Subclasses");

            migrationBuilder.DropColumn(
                name: "SpellSchools",
                table: "Subclasses");
        }
    }
}
