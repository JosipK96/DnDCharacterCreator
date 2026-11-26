using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CharacterCreatorApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RaceAbilityChoiceCaps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // A race's or subrace's ability choice is +1 each until the reference data is reloaded
            // with the caps of the books that raise one ability by 2
            migrationBuilder.AddColumn<int>(
                name: "AbilityChoiceMaxPerAbility",
                table: "Subraces",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "AbilityChoiceMinPerAbility",
                table: "Subraces",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "AbilityChoiceMaxPerAbility",
                table: "Races",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "AbilityChoiceMinPerAbility",
                table: "Races",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AbilityChoiceMaxPerAbility",
                table: "Subraces");

            migrationBuilder.DropColumn(
                name: "AbilityChoiceMinPerAbility",
                table: "Subraces");

            migrationBuilder.DropColumn(
                name: "AbilityChoiceMaxPerAbility",
                table: "Races");

            migrationBuilder.DropColumn(
                name: "AbilityChoiceMinPerAbility",
                table: "Races");
        }
    }
}
