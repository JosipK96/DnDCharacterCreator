using CharacterCreatorApp.Domain.DomainModels.Feats;
using CharacterCreatorApp.Infrastructure.Seed.Rows;

using Microsoft.Extensions.Logging;

namespace CharacterCreatorApp.Infrastructure.Seed.Seeders
{
    public class FeatSeeder(ILogger<FeatSeeder> logger) : IEntitySeeder
    {
        public int Priority => 6;

        public async Task SeedAsync(ApplicationDbContext context, string dataPath, CancellationToken cancellationToken)
        {
            // Both editions; the generator has already parsed the prerequisites and worked out
            // each feat's edition from its book
            var rows = await SeedFile.ReadListAsync<FeatSeedRow>(Path.Combine(dataPath, "Feats.json"), cancellationToken);

            await context.Feats.AddRangeAsync(rows.Select(row => new Feat
            {
                Id = row.Id,
                Name = row.Name,
                Description = row.Description,
                Edition = row.Edition,
                Source = row.Source,
                Category = row.Category,
                Prerequisites = row.Prerequisites,
                IsRepeatable = row.IsRepeatable,
                AbilityIncrease = row.AbilityIncrease,
                GrantedProficiencyIds = row.GrantedProficiencyIds,
                GrantedLanguageIds = row.GrantedLanguageIds,
                GrantedSpellIds = row.GrantedSpellIds,
                Choice = row.Choice,
                SpellChoice = row.SpellChoice,
            }), cancellationToken);

            logger.LogInformation("Seeded {Count} feats: {Editions}.", rows.Count,
                string.Join(", ", rows.CountBy(r => r.Edition).Select(kvp => $"{kvp.Value} for {kvp.Key}")));
        }
    }
}
