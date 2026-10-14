using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;

namespace CharacterCreatorApp.Domain.ValueObjects;

public class AbilityScore
{
    /// <summary>The most an increase raises a score to, an Epic Boon's aside (30).</summary>
    public const int Maximum = 20;

    public string Id { get; }
    public string Name { get; }
    public string Abbreviation { get; }
    public string Description { get; }

    private AbilityScore(string id, string name, string abbreviation, string description)
    {
        Id = id;
        Name = name;
        Abbreviation = abbreviation;
        Description = description;
    }

    public static readonly AbilityScore STR = new("STR", "Strength", "Str", "Measures physical power.");
    public static readonly AbilityScore DEX = new("DEX", "Dexterity", "Dex", "Measures agility.");
    public static readonly AbilityScore CON = new("CON", "Constitution", "Con", "Measures endurance.");
    public static readonly AbilityScore INT = new("INT", "Intelligence", "Int", "Measures reasoning.");
    public static readonly AbilityScore WIS = new("WIS", "Wisdom", "Wis", "Measures perception.");
    public static readonly AbilityScore CHA = new("CHA", "Charisma", "Cha", "Measures force of personality.");

    public static IReadOnlyList<AbilityScore> All => new[] { STR, DEX, CON, INT, WIS, CHA };

    /// <summary>
    /// A score raised by an increase, which stops at the maximum and never lowers a score
    /// already above it.
    /// </summary>
    public static int Raise(int score, int points, int maximum = Maximum) =>
        Math.Max(score, Math.Min(maximum, score + points));

    /// <summary>
    /// Every spelling that may arrive from seed data, API requests or older rows,
    /// mapped to the ability it names. Lookup is case-insensitive, so "STR", "str",
    /// "Strength" and "strength" all resolve to the same ability.
    /// </summary>
    private static readonly FrozenDictionary<string, AbilityScore> ByKey =
        All.SelectMany(a => new[] { a.Id, a.Name, a.Abbreviation }, (a, key) => (key, a))
           .DistinctBy(x => x.key, StringComparer.OrdinalIgnoreCase)
           .ToFrozenDictionary(x => x.key, x => x.a, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The ability any known spelling names. This used to accept only the canonical id, so the
    /// race bonuses seeded as "con" and "str" made the races endpoint throw.
    /// </summary>
    public static AbilityScore FromId(string id) =>
        ByKey.TryGetValue(id, out var ability)
            ? ability
            : throw new ArgumentException($"Invalid AbilityScore id: {id}", nameof(id));

    /// <summary>
    /// Resolves any known spelling of an ability to its canonical id (STR, DEX, ...).
    /// Returns false for anything unrecognised.
    /// </summary>
    public static bool TryNormalize(string? key, [NotNullWhen(true)] out string? id)
    {
        if (key is not null && ByKey.TryGetValue(key, out var ability))
        {
            id = ability.Id;
            return true;
        }

        id = null;
        return false;
    }

    /// <summary>
    /// Resolves any known spelling of an ability to its canonical id (STR, DEX, ...).
    /// </summary>
    public static string Normalize(string key) =>
        TryNormalize(key, out var id)
            ? id
            : throw new ArgumentException($"Invalid ability score key: {key}", nameof(key));
}
