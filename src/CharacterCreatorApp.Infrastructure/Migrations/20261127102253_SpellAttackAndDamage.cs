using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CharacterCreatorApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SpellAttackAndDamage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AttackType",
                table: "Spells",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<List<string>>(
                name: "CantripDamageDice",
                table: "Spells",
                type: "text[]",
                nullable: false,
                defaultValueSql: "'{}'");

            migrationBuilder.AddColumn<string>(
                name: "DamageType",
                table: "Spells",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SaveAbilityId",
                table: "Spells",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttackType",
                table: "Spells");

            migrationBuilder.DropColumn(
                name: "CantripDamageDice",
                table: "Spells");

            migrationBuilder.DropColumn(
                name: "DamageType",
                table: "Spells");

            migrationBuilder.DropColumn(
                name: "SaveAbilityId",
                table: "Spells");
        }
    }
}
