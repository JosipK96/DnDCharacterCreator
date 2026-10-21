using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CharacterCreatorApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SpellSlots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PactMagicSlots",
                table: "SubclassSpellProgressions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<List<int>>(
                name: "SpellSlots",
                table: "SubclassSpellProgressions",
                type: "integer[]",
                nullable: false,
                // The rows are reference data, read again from the seed files once the hash changes
                defaultValueSql: "'{}'");

            migrationBuilder.AddColumn<int>(
                name: "PactMagicSlots",
                table: "ClassSpellProgressions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<List<int>>(
                name: "SpellSlots",
                table: "ClassSpellProgressions",
                type: "integer[]",
                nullable: false,
                // The rows are reference data, read again from the seed files once the hash changes
                defaultValueSql: "'{}'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PactMagicSlots",
                table: "SubclassSpellProgressions");

            migrationBuilder.DropColumn(
                name: "SpellSlots",
                table: "SubclassSpellProgressions");

            migrationBuilder.DropColumn(
                name: "PactMagicSlots",
                table: "ClassSpellProgressions");

            migrationBuilder.DropColumn(
                name: "SpellSlots",
                table: "ClassSpellProgressions");
        }
    }
}
