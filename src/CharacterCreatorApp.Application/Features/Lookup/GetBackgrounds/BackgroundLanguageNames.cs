using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;

namespace CharacterCreatorApp.Application.Features.Lookup.GetBackgrounds;

/// <summary>
/// Puts the languages a background names ("common" in the 2024 data) in the display name of its
/// edition's language ("Common"). A language the edition's book does not have, such as one a
/// background names from another book, keeps the name the data gives it.
/// </summary>
internal static class BackgroundLanguageNames
{
    public static async Task ApplyAsync(
        IEnumerable<BackgroundReadModel> backgrounds, ILanguageRepository languageRepository, CancellationToken cancellationToken)
    {
        var languagesByEdition = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);

        foreach (var background in backgrounds)
        {
            if (!languagesByEdition.TryGetValue(background.Edition, out var namesByLowerName))
            {
                var languages = await languageRepository.GetAllForEditionAsync(background.Edition, cancellationToken);
                namesByLowerName = languages
                    .DistinctBy(l => l.Name, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(l => l.Name, l => l.Name, StringComparer.OrdinalIgnoreCase);
                languagesByEdition[background.Edition] = namesByLowerName;
            }

            background.Languages = background.Languages.ConvertAll(name => namesByLowerName.GetValueOrDefault(name, name));
        }
    }
}
