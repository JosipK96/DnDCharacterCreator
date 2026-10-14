using CharacterCreatorApp.Domain.DomainModels.Languages;

namespace CharacterCreatorApp.SeedDataGenerator;

/// <summary>
/// Reads languages.json into Languages.json: every book's languages under "name-book" ids
/// ("common-phb", "common-xphb"), a character drawing on its own edition's book. A language
/// without a type is a creature's, counted as standard; the 2024 book's "rare" ones are kept as
/// exotic, the word the 2014 book uses for the same list.
/// </summary>
internal sealed class LanguageImporter(FiveEToolsSource source)
{
    private readonly FiveEToolsSource _source = source;

    public List<Language> Import() =>
        _source.Load("languages.json").Objects("language")
            .Where(language => language["_copy"] == null)
            .Select(language => new Language
            {
                Id = $"{Slug.Of(language.Str("name")!)}-{language.Str("source")!.ToLowerInvariant()}",
                Name = language.Str("name")!,
                Type = TypeOf(language.Str("type")),
                Script = language.Str("script"),
                Source = language.Str("source")!,
            })
            .OrderBy(language => language.Id, StringComparer.Ordinal)
            .ToList();

    private static string TypeOf(string? type) => type switch
    {
        null or "" => Language.StandardType,
        "rare" => Language.ExoticType,
        _ => type,
    };
}
