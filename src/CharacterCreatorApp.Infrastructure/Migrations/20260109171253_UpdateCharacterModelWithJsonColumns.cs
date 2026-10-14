using System.Collections.Generic;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CharacterCreatorApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCharacterModelWithJsonColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ""Characters"" 
                ALTER COLUMN ""ProficiencyIds"" TYPE jsonb 
                USING to_jsonb(""ProficiencyIds"");
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""Characters"" 
                ALTER COLUMN ""LanguageIds"" TYPE jsonb 
                USING to_jsonb(""LanguageIds"");
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""Characters"" 
                ALTER COLUMN ""FeatureIds"" TYPE jsonb 
                USING to_jsonb(""FeatureIds"");
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""Characters"" 
                ALTER COLUMN ""SpellIds"" TYPE jsonb 
                USING to_jsonb(""SpellIds"");
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ""Characters"" 
                ALTER COLUMN ""ProficiencyIds"" TYPE text[] 
                USING (
                    SELECT ARRAY(SELECT jsonb_array_elements_text(""ProficiencyIds""))
                );
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""Characters"" 
                ALTER COLUMN ""LanguageIds"" TYPE text[] 
                USING (
                    SELECT ARRAY(SELECT jsonb_array_elements_text(""LanguageIds""))
                );
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""Characters"" 
                ALTER COLUMN ""FeatureIds"" TYPE text[] 
                USING (
                    SELECT ARRAY(SELECT jsonb_array_elements_text(""FeatureIds""))
                );
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""Characters"" 
                ALTER COLUMN ""SpellIds"" TYPE text[] 
                USING (
                    SELECT ARRAY(SELECT jsonb_array_elements_text(""SpellIds""))
                );
            ");
        }
    }
}
