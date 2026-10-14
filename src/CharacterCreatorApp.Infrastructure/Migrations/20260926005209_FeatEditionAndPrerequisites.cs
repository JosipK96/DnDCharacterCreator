using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CharacterCreatorApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FeatEditionAndPrerequisites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Feats now carry their edition and the prerequisites the seed data spells out, and
            // seeding only fills an empty table: emptying it lets the seeder refill it at startup
            // with both. No foreign key points at a feat; characters and drafts keep feat ids in
            // lists, and the ids do not change.
            migrationBuilder.Sql("DELETE FROM \"Feats\";");

            migrationBuilder.DropIndex(
                name: "IX_Feats_RequiredClassId",
                table: "Feats");

            migrationBuilder.DropIndex(
                name: "IX_Feats_RequiredLevel",
                table: "Feats");

            migrationBuilder.DropIndex(
                name: "IX_Feats_RequiredRaceId",
                table: "Feats");

            migrationBuilder.DropColumn(
                name: "MinimumAbilityScore",
                table: "Feats");

            migrationBuilder.DropColumn(
                name: "RequiredAbilityScore",
                table: "Feats");

            migrationBuilder.DropColumn(
                name: "RequiredClassId",
                table: "Feats");

            migrationBuilder.DropColumn(
                name: "RequiredLevel",
                table: "Feats");

            migrationBuilder.DropColumn(
                name: "RequiredRaceId",
                table: "Feats");

            migrationBuilder.AddColumn<string>(
                name: "Edition",
                table: "Feats",
                type: "character varying(4)",
                maxLength: 4,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Prerequisites",
                table: "Feats",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Edition",
                table: "Feats");

            migrationBuilder.DropColumn(
                name: "Prerequisites",
                table: "Feats");

            migrationBuilder.AddColumn<int>(
                name: "MinimumAbilityScore",
                table: "Feats",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequiredAbilityScore",
                table: "Feats",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequiredClassId",
                table: "Feats",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RequiredLevel",
                table: "Feats",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequiredRaceId",
                table: "Feats",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Feats_RequiredClassId",
                table: "Feats",
                column: "RequiredClassId");

            migrationBuilder.CreateIndex(
                name: "IX_Feats_RequiredLevel",
                table: "Feats",
                column: "RequiredLevel");

            migrationBuilder.CreateIndex(
                name: "IX_Feats_RequiredRaceId",
                table: "Feats",
                column: "RequiredRaceId");
        }
    }
}
