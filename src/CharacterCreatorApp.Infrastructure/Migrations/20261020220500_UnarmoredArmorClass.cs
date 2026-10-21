using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CharacterCreatorApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UnarmoredArmorClass : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UnarmoredArmorClass",
                table: "Subclasses",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UnarmoredArmorClass",
                table: "Classes",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UnarmoredArmorClass",
                table: "Subclasses");

            migrationBuilder.DropColumn(
                name: "UnarmoredArmorClass",
                table: "Classes");
        }
    }
}
