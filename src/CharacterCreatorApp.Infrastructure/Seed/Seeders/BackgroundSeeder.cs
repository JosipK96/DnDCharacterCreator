using CharacterCreatorApp.Domain.DomainModels.Backgrounds;
using CharacterCreatorApp.Infrastructure.Seed.Rows;

using Microsoft.Extensions.Logging;

namespace CharacterCreatorApp.Infrastructure.Seed.Seeders
{
    public class BackgroundSeeder(ILogger<BackgroundSeeder> logger) : IEntitySeeder
    {
        public int Priority => 3;

        public async Task SeedAsync(ApplicationDbContext context, string dataPath, CancellationToken cancellationToken)
        {
            // Both editions; the 2014 features are in BackgroundFeatures.json, which FeatureSeeder reads
            var rows = await SeedFile.ReadListAsync<BackgroundSeedRow>(Path.Combine(dataPath, "Backgrounds.json"), cancellationToken);

            await context.Backgrounds.AddRangeAsync(rows.Select(row => new Background
            {
                Id = row.Id,
                Name = row.Name,
                Edition = row.Edition,
                Source = row.Source,
                Languages = row.Languages,
                AbilityScoreOptions = row.AbilityScoreOptions,
                FeatId = row.FeatId,
                FeatureId = row.FeatureId,
                BackgroundProficiencies = row.ProficiencyIds.ConvertAll(id => new BackgroundProficiency { BackgroundId = row.Id, ProficiencyId = id }),
            }), cancellationToken);

            logger.LogInformation("Seeded {Count} backgrounds: {Editions}.", rows.Count,
                string.Join(", ", rows.CountBy(r => r.Edition).Select(kvp => $"{kvp.Value} for {kvp.Key}")));
        }
    }
}
