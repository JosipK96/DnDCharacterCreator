using CharacterCreatorApp.Domain.DomainModels.Languages;

using Microsoft.Extensions.Logging;

namespace CharacterCreatorApp.Infrastructure.Seed.Seeders;

public class LanguageSeeder(ILogger<LanguageSeeder> logger) : IEntitySeeder
{
    public int Priority => 1;

    public async Task SeedAsync(ApplicationDbContext context, string dataPath, CancellationToken cancellationToken)
    {
        var languages = await SeedFile.ReadListAsync<Language>(Path.Combine(dataPath, "Languages.json"), cancellationToken);

        await context.Languages.AddRangeAsync(languages, cancellationToken);
        logger.LogInformation("Seeded {Count} languages.", languages.Count);
    }
}