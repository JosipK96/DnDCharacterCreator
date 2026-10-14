namespace CharacterCreatorApp.Infrastructure.Seed.Rows;

// The shapes of the spell seed files, written by the generator and read by SpellSeeder.

/// <summary>One entry of Spells/Spells.json.</summary>
public sealed record SpellSeedRow
{
    public required string Id { get; init; }
    public required string Name { get; init; }

    /// <summary>The code of the book it comes from ("PHB", "XGE"), a row of Books.json.</summary>
    public required string Source { get; init; }
    public int Level { get; init; }
    public required string School { get; init; }
    public int CastingTimeNumber { get; init; }

    /// <summary>"action", "bonus", "reaction", "minute" or "hour".</summary>
    public required string CastingTimeUnit { get; init; }

    /// <summary>"point" for a distance, or the shape of an area around the caster ("cone").</summary>
    public required string RangeType { get; init; }

    public string? RangeDistanceType { get; init; }
    public int? RangeDistanceAmount { get; init; }
    public bool HasVerbalComponent { get; init; }
    public bool HasSomaticComponent { get; init; }
    public bool HasMaterialComponent { get; init; }
    public string? Material { get; init; }

    /// <summary>"instant", "timed", "permanent" or "special".</summary>
    public required string DurationType { get; init; }

    public int? DurationAmount { get; init; }
    public string? DurationUnit { get; init; }
    public bool IsConcentration { get; init; }
    public bool IsRitual { get; init; }
    public required string Description { get; init; }
}

/// <summary>One entry of Spells/SpellClasses.json: the spell is on the class's list.</summary>
public sealed record SpellClassSeedRow
{
    public required string SpellId { get; init; }
    public required string ClassId { get; init; }
}

/// <summary>One entry of Spells/SpellSubclasses.json.</summary>
public sealed record SpellSubclassSeedRow
{
    public required string SpellId { get; init; }
    public required string SubclassId { get; init; }

    /// <summary>The character level from which the subclass gives the spell.</summary>
    public int Level { get; init; }

    /// <summary>
    /// True for a spell the character has without picking it (a domain or oath spell). False
    /// for one the subclass only adds to the list the character picks from (a warlock
    /// patron's expanded spells).
    /// </summary>
    public bool IsAlwaysPrepared { get; init; }
}
