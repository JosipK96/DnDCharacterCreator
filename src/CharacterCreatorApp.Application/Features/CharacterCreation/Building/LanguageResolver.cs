using CharacterCreatorApp.Application.ReadModels;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.Building;

/// <summary>
/// Turns the ways the seed data names a language into the ids of the character's edition: PHB
/// ones for 2014, XPHB ones for 2024 ("common-xphb"). Race languages are display names
/// ("Common"), background and feat languages lower-case names ("goblin", "thieves' cant") with
/// "any" standing for a free choice, and drafts hold ids ("common-phb"). A token that is neither
/// a name nor an id of the edition's languages resolves to nothing: the "any" placeholders, and
/// the few languages some backgrounds name from other books.
/// </summary>
public static class LanguageResolver
{
    public static List<string> ResolveIds(IEnumerable<string> namesOrIds, IReadOnlyCollection<LanguageReadModel> languages)
    {
        var idsByName = languages
            .DistinctBy(l => l.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(l => l.Name, l => l.Id, StringComparer.OrdinalIgnoreCase);

        var knownIds = languages.Select(l => l.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

        string? Resolve(string token)
        {
            if (knownIds.TryGetValue(token, out var id))
            {
                return id;
            }

            return idsByName.TryGetValue(token, out var byName) ? byName : null;
        }

        return namesOrIds
            .Select(Resolve)
            .OfType<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
