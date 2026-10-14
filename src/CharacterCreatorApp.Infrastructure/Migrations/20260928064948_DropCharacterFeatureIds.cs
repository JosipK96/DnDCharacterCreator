using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CharacterCreatorApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DropCharacterFeatureIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Nothing is lost: a character's features are worked out from its class, subclass,
            // level and background each time the sheet is read (CharacterFeatures)
            migrationBuilder.DropColumn(
                name: "FeatureIds",
                table: "Characters");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The scaffolded default was "", which is not valid jsonb
            migrationBuilder.AddColumn<string>(
                name: "FeatureIds",
                table: "Characters",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");
        }
    }
}
