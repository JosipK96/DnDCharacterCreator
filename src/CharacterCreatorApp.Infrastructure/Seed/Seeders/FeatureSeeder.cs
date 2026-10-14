using CharacterCreatorApp.Domain.DomainModels.Classes;
using CharacterCreatorApp.Infrastructure.Seed.Rows;

using Microsoft.Extensions.Logging;

namespace CharacterCreatorApp.Infrastructure.Seed.Seeders
{
    public class FeatureSeeder(ILogger<FeatureSeeder> logger) : IEntitySeeder
    {
        public int Priority => 5;

        public async Task SeedAsync(ApplicationDbContext context, string dataPath, CancellationToken cancellationToken)
        {
            // Class, subclass and 2014 background features, all generated from 5etools
            var rows = (await SeedFile.ReadListAsync<FeatureSeedRow>(Path.Combine(dataPath, "ClassFeatures.json"), cancellationToken))
                .Concat(await SeedFile.ReadListAsync<FeatureSeedRow>(Path.Combine(dataPath, "BackgroundFeatures.json"), cancellationToken))
                .ToList();

            await context.Features.AddRangeAsync(rows.Select(row => new Feature
            {
                Id = row.Id,
                Name = row.Name,
                Description = row.Description,
                Level = row.Level,
                ClassId = row.ClassId,
                SubclassId = row.SubclassId,
                Type = row.Type
            }), cancellationToken);

            logger.LogInformation("Seeded {Count} features.", rows.Count);
        }
    }
}
