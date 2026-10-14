using System.Text.Json.Nodes;

namespace CharacterCreatorApp.SeedDataGenerator;

/// <summary>
/// The folder of raw 5etools data (the "data" folder of a 5etools release). Files are read once
/// and kept, since the class files are looked through several times.
/// </summary>
internal sealed class FiveEToolsSource(string root)
{
    /// <summary>
    /// The classes the app offers. The class folder also has the Mystic (Unearthed Arcana) and
    /// the Sidekick classes, which are not player classes.
    /// </summary>
    public static readonly IReadOnlyList<string> ClassFileNames =
    [
        "artificer", "barbarian", "bard", "cleric", "druid", "fighter", "monk",
        "paladin", "ranger", "rogue", "sorcerer", "warlock", "wizard",
    ];

    private readonly string _root = Directory.Exists(root)
        ? root
        : throw new DirectoryNotFoundException($"No 5etools data folder at {root}");

    private readonly Dictionary<string, JsonObject> _files = [];

    public JsonObject Load(string relativePath)
    {
        if (_files.TryGetValue(relativePath, out var cached))
        {
            return cached;
        }

        var path = Path.Combine(_root, relativePath);
        var json = JsonNode.Parse(File.ReadAllText(path))?.AsObject()
            ?? throw new InvalidDataException($"{path} is not a JSON object");

        _files[relativePath] = json;
        return json;
    }

    public IEnumerable<JsonObject> ClassFiles() =>
        ClassFileNames.Select(name => Load(Path.Combine("class", $"class-{name}.json")));

    /// <summary>Every book's spells: spells-phb.json, spells-xphb.json, spells-xge.json and the rest.</summary>
    public IEnumerable<JsonObject> SpellFiles() =>
        Directory.EnumerateFiles(Path.Combine(_root, "spells"), "spells-*.json")
            .Order(StringComparer.Ordinal)
            .Select(path => Load(Path.GetRelativePath(_root, path)));
}
