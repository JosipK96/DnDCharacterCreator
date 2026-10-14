namespace CharacterCreatorApp.Domain.Services;

/// <summary>
/// The choices that add hit points at every level. The ids are the seed data's, and
/// SeedDataIntegrityTests checks they still exist there.
/// </summary>
public static class HitPointBonuses
{
    /// <summary>Dwarven Toughness: 1 more hit point per level, for a 2014 hill dwarf...</summary>
    public const string HillDwarfSubraceId = "hill_dwarf";

    /// <summary>...and for every 2024 dwarf, whose species has it rather than a subrace.</summary>
    public const string Dwarf2024RaceId = "dwarf_2024";

    /// <summary>
    /// Draconic Resilience: 1 more hit point per sorcerer level. The 2024 subclass words it as 3
    /// at level 3 and 1 a level after, which comes to the same once the subclass is chosen.
    /// </summary>
    public static readonly IReadOnlyList<string> DraconicSorcererSubclassIds = ["draconic", "draconic-2024"];

    /// <summary>The Tough feat, in both editions the data carries: 2 more hit points per level.</summary>
    public static readonly IReadOnlyList<string> ToughFeatIds = ["tough_2014", "tough_2024"];

    public static int PerLevel(string raceId, string? subraceId, string? subclassId, IEnumerable<string> featIds)
    {
        ArgumentNullException.ThrowIfNull(featIds);

        var bonus = 0;

        if (subraceId == HillDwarfSubraceId || raceId == Dwarf2024RaceId)
        {
            bonus += 1;
        }

        // Characters have one class, so every level is a sorcerer level
        if (subclassId != null && DraconicSorcererSubclassIds.Contains(subclassId))
        {
            bonus += 1;
        }

        if (featIds.Any(ToughFeatIds.Contains))
        {
            bonus += 2;
        }

        return bonus;
    }
}
