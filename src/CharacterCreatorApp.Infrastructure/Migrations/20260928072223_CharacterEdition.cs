using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CharacterCreatorApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CharacterEdition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Every character and draft made so far was built from the 2014 data, the only data
            // there was. The scaffolded default was "", which is no edition at all.
            migrationBuilder.AddColumn<string>(
                name: "Edition",
                table: "Characters",
                type: "character varying(4)",
                maxLength: 4,
                nullable: false,
                defaultValue: "2014");

            migrationBuilder.AddColumn<string>(
                name: "Edition",
                table: "CharacterDrafts",
                type: "character varying(4)",
                maxLength: 4,
                nullable: false,
                defaultValue: "2014");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Edition",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Edition",
                table: "CharacterDrafts");
        }
    }
}
