using CharacterCreatorApp.Domain.DomainModels.Proficiencies;
using CharacterCreatorApp.Infrastructure.Seed;

namespace CharacterCreatorApp.SeedDataGenerator;

/// <summary>
/// Maps 5etools' proficiency names ("light", "thieves' tools", "{@item dagger|phb|daggers}") onto
/// the ids in Proficiencies.json. That file is maintained by hand and the generator only reads
/// it, so an id never changes under a character that has it. A name it cannot place is collected
/// and reported instead of being dropped.
/// </summary>
internal sealed class ProficiencyIndex
{
    // 5etools' short names for the armour and weapon categories
    private static readonly Dictionary<string, string> CategoryNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["light"] = "Light Armor",
        ["medium"] = "Medium Armor",
        ["heavy"] = "Heavy Armor",
        ["shield"] = "Shields",
        ["shields"] = "Shields",
        ["simple"] = "Simple Weapons",
        ["martial"] = "Martial Weapons",

        // The 2024 Monk and Rogue get part of the martial weapons, described in a sentence
        ["Martial weapons that have the Light property"] = "Light Martial Weapons",
        ["Martial weapons that have the Finesse or Light property"] = "Finesse or Light Martial Weapons",

        // The 2024 Tavern Brawler
        ["improvised"] = "Improvised Weapons",
    };

    private readonly Dictionary<string, Proficiency> _byName;

    private ProficiencyIndex(IEnumerable<Proficiency> proficiencies)
    {
        _byName = proficiencies.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
    }

    public SortedSet<string> Unknown { get; } = [];

    public IEnumerable<Proficiency> Skills => _byName.Values.Where(p => p.Type == "skill");

    /// <summary>Every skill and every tool, instrument and gaming set: what "any skill or tool" offers.</summary>
    public IEnumerable<Proficiency> SkillsAndTools =>
        _byName.Values.Where(p => p.Type is "skill" or "tool" or "musical-instrument" or "gaming_set");

    public static async Task<ProficiencyIndex> LoadAsync(string dataPath)
    {
        var proficiencies = await SeedFile.ReadListAsync<Proficiency>(Path.Combine(dataPath, "Proficiencies.json"), CancellationToken.None);
        return new ProficiencyIndex(proficiencies);
    }

    /// <summary>The id for a name, or null (and the name is noted) when there is none.</summary>
    public string? IdOf(string name)
    {
        var plain = FiveEToolsMarkup.ToPlainText(name).Trim();

        if (CategoryNames.TryGetValue(plain, out var category))
        {
            plain = category;
        }

        if (_byName.TryGetValue(plain, out var proficiency))
        {
            return proficiency.Id;
        }

        Unknown.Add(plain);
        return null;
    }

    /// <summary>
    /// The id for a key the way races name weapons and tools, "longsword|phb": one item and its
    /// book, where Proficiencies.json names some weapons in the plural ("Longswords").
    /// </summary>
    public string? IdOfItem(string key)
    {
        var name = key.Split('|')[0];

        return _byName.TryGetValue(name + "s", out var plural) && !_byName.ContainsKey(name)
            ? plural.Id
            : IdOf(name);
    }
}
