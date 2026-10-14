using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CharacterCreatorApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DraftAllowedSources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<string>>(
                name: "AllowedSources",
                table: "CharacterDrafts",
                type: "text[]",
                nullable: true);

            // A draft saved before allows its edition's Player's Handbook, as a new one does
            migrationBuilder.Sql("""
                UPDATE "CharacterDrafts"
                SET "AllowedSources" = CASE WHEN "Edition" = '2014' THEN ARRAY['PHB'] ELSE ARRAY['XPHB'] END;
                """);

            migrationBuilder.AlterColumn<List<string>>(
                name: "AllowedSources",
                table: "CharacterDrafts",
                type: "text[]",
                nullable: false,
                oldClrType: typeof(List<string>),
                oldType: "text[]",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowedSources",
                table: "CharacterDrafts");
        }
    }
}
