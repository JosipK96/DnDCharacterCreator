using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CharacterCreatorApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCharacterFeatIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // EF scaffolds defaultValue: "" for this column, which Postgres rejects as invalid
            // jsonb. Existing characters have no feats, so an empty JSON array is the default.
            migrationBuilder.AddColumn<string>(
                name: "FeatIds",
                table: "Characters",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FeatIds",
                table: "Characters");
        }
    }
}
