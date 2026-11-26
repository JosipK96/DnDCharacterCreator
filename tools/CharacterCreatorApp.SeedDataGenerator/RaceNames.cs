namespace CharacterCreatorApp.SeedDataGenerator;

/// <summary>
/// The ids of the imported races and subraces by the names a feat's prerequisite gives them:
/// "elf", a subrace as {"name": "gnome", "subrace": "deep"}, or one written "Vampire (Ixalan)".
/// A race from a book other than the Player's Handbooks has its book in its id
/// ("vampire_psz"), so the names alone do not give it. Filled by <see cref="RaceImporter"/>.
/// </summary>
internal sealed class RaceNames
{
    private readonly Dictionary<string, List<string>> _races = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<string>> _subraces = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<string>> _subracesByFullName = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Nothing imported: every name falls back to its slug, as the parser's own tests use.</summary>
    public static RaceNames None { get; } = new();

    public void AddRace(string edition, string name, string id) => Add(_races, $"{edition}|{name}", id);

    /// <summary>A subrace, under its race's name and its own as 5etools gives it ("Deep", "Ixalan").</summary>
    public void AddSubrace(string edition, string raceName, string subraceName, string id)
    {
        Add(_subraces, $"{edition}|{raceName}|{subraceName}", id);

        // How 5etools writes a subrace as one name, in a prerequisite too: "Vampire (Ixalan)"
        Add(_subracesByFullName, $"{edition}|{raceName} ({subraceName})", id);
    }

    /// <summary>The races of that name in the edition; none when the data has no such race.</summary>
    public IReadOnlyList<string> RaceIds(string edition, string name) =>
        _races.GetValueOrDefault($"{edition}|{name}") ?? [];

    /// <summary>The subraces of that race with that name; none when the data has no such subrace.</summary>
    public IReadOnlyList<string> SubraceIds(string edition, string raceName, string subraceName) =>
        _subraces.GetValueOrDefault($"{edition}|{raceName}|{subraceName}") ?? [];

    /// <summary>The subraces written "Race (Subrace)" as one name: "Vampire (Ixalan)".</summary>
    public IReadOnlyList<string> SubraceIdsByFullName(string edition, string fullName) =>
        _subracesByFullName.GetValueOrDefault($"{edition}|{fullName}") ?? [];

    private static void Add(Dictionary<string, List<string>> ids, string key, string id)
    {
        if (!ids.TryGetValue(key, out var list))
        {
            ids[key] = list = [];
        }

        if (!list.Contains(id))
        {
            list.Add(id);
        }
    }
}
