using CharacterCreatorApp.Domain.DomainModels.Proficiencies;

using Microsoft.Extensions.Logging;

namespace CharacterCreatorApp.Infrastructure.Seed.Seeders
{
    public class ProficiencySeeder(ILogger<ProficiencySeeder> logger) : IEntitySeeder
    {
        public int Priority => 1;

        public async Task SeedAsync(ApplicationDbContext context, string dataPath, CancellationToken cancellationToken)
        {
            var proficiencies = await SeedFile.ReadListAsync<Proficiency>(Path.Combine(dataPath, "Proficiencies.json"), cancellationToken);

            await context.Proficiencies.AddRangeAsync(proficiencies, cancellationToken);
            logger.LogInformation("Seeded {Count} proficiencies.", proficiencies.Count);

            var typeBreakdown = proficiencies.CountBy(p => p.Type);

            logger.LogInformation("Proficiency breakdown: {Breakdown}", string.Join(", ", typeBreakdown.Select(kvp => $"{kvp.Key}: {kvp.Value}")));
        }
    }
}
