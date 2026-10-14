using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CharacterCreatorApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BackgroundsFromData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BackgroundAbilityIncreases",
                table: "CharacterDrafts",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.AlterColumn<string>(
                name: "FeatureId",
                table: "Backgrounds",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<List<string>>(
                name: "AbilityScoreOptions",
                table: "Backgrounds",
                type: "text[]",
                nullable: false,
                defaultValueSql: "'{}'");

            migrationBuilder.AddColumn<string>(
                name: "Edition",
                table: "Backgrounds",
                type: "character varying(4)",
                maxLength: 4,
                nullable: false,
                defaultValue: "2014");

            migrationBuilder.AddColumn<string>(
                name: "FeatId",
                table: "Backgrounds",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Backgrounds_Edition",
                table: "Backgrounds",
                column: "Edition");

            migrationBuilder.CreateIndex(
                name: "IX_Backgrounds_FeatId",
                table: "Backgrounds",
                column: "FeatId");

            migrationBuilder.AddForeignKey(
                name: "FK_Backgrounds_Feats_FeatId",
                table: "Backgrounds",
                column: "FeatId",
                principalTable: "Feats",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Backgrounds_Feats_FeatId",
                table: "Backgrounds");

            migrationBuilder.DropIndex(
                name: "IX_Backgrounds_Edition",
                table: "Backgrounds");

            migrationBuilder.DropIndex(
                name: "IX_Backgrounds_FeatId",
                table: "Backgrounds");

            migrationBuilder.DropColumn(
                name: "BackgroundAbilityIncreases",
                table: "CharacterDrafts");

            migrationBuilder.DropColumn(
                name: "AbilityScoreOptions",
                table: "Backgrounds");

            migrationBuilder.DropColumn(
                name: "Edition",
                table: "Backgrounds");

            migrationBuilder.DropColumn(
                name: "FeatId",
                table: "Backgrounds");

            migrationBuilder.AlterColumn<string>(
                name: "FeatureId",
                table: "Backgrounds",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
