using CharacterCreatorApp.Domain.DomainModels.Classes;
using CharacterCreatorApp.Domain.DomainModels.Proficiencies;

namespace CharacterCreatorApp.Infrastructure.Seed.Rows;

// The shapes of the class seed files. tools/CharacterCreatorApp.SeedDataGenerator writes them
// from 5etools and the seeders read them, so both sides agree on the format at compile time.

/// <summary>One entry of Classes.json.</summary>
public sealed record ClassSeedRow
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Edition { get; init; }

    /// <summary>The code of the book it comes from ("PHB", "XGE"), a row of Books.json.</summary>
    public required string Source { get; init; }
    public required string HitDie { get; init; }
    public int SubclassLevel { get; init; }
    public List<int> AsiLevels { get; init; } = [];
    public List<string> SavingThrowAbilityIds { get; init; } = [];

    /// <summary>A barbarian's or monk's Unarmored Defense; null for every other class.</summary>
    public UnarmoredArmorClass? UnarmoredArmorClass { get; init; }

    public List<string> ProficiencyIds { get; init; } = [];
    public ProficiencyChoiceSeedRow? ProficiencyChoices { get; init; }
    public List<ToolChoice> ToolChoices { get; init; } = [];
    public SpellcastingSeedRow? Spellcasting { get; init; }
    public List<SpellProgressionSeedRow> SpellProgression { get; init; } = [];
}

public sealed record ProficiencyChoiceSeedRow
{
    public required string Type { get; init; }
    public int Count { get; init; }
    public List<string> Options { get; init; } = [];
}

public sealed record SpellcastingSeedRow
{
    /// <summary>The ability's full name, "Intelligence".</summary>
    public required string Ability { get; init; }

    public bool IsPreparedCaster { get; init; }

    /// <summary>
    /// 1 or 2 for a 2014 class that prepares its modifier plus its level (or half of it) in spells;
    /// null otherwise. See Spellcasting.PreparedSpellsLevelDivisor.
    /// </summary>
    public int? PreparedSpellsLevelDivisor { get; init; }
}

/// <summary>What a caster of the class or subclass has at one character level.</summary>
public sealed record SpellProgressionSeedRow
{
    public int Level { get; init; }
    public int CantripsKnown { get; init; }

    /// <summary>
    /// The levelled spells the player picks: spells known, a 2024 class's prepared spells, or the
    /// spells in a wizard's spellbook. 0 for a 2014 class whose prepared spells follow its
    /// modifier and level instead (SpellcastingSeedRow.PreparedSpellsLevelDivisor).
    /// </summary>
    public int SpellsKnown { get; init; }

    public int MaxSpellLevel { get; init; }

    /// <summary>The spell slots per spell level, index 0 for 1st level; empty for a warlock.</summary>
    public List<int> SpellSlots { get; init; } = [];

    /// <summary>A warlock's Pact Magic slots, all of MaxSpellLevel; 0 otherwise.</summary>
    public int PactMagicSlots { get; init; }
}

/// <summary>One entry of Subclasses.json.</summary>
public sealed record SubclassSeedRow
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string ClassId { get; init; }

    /// <summary>The code of the book it comes from ("PHB", "XGE"), a row of Books.json.</summary>
    public required string Source { get; init; }

    /// <summary>Set for a subclass that casts spells of its own, like the Eldritch Knight.</summary>
    public string? SpellcastingAbility { get; init; }

    /// <summary>
    /// A class whose whole spell list the subclass may pick from: the Wizard's for an Eldritch
    /// Knight or Arcane Trickster, the Cleric's for a Divine Soul sorcerer.
    /// </summary>
    public string? SpellListClassId { get; init; }

    public List<SpellProgressionSeedRow> SpellProgression { get; init; } = [];

    /// <summary>
    /// The schools the subclass's levelled spells must come from ("Abjuration", "Evocation"), or
    /// empty when any school will do.
    /// </summary>
    public List<string> SpellSchools { get; init; } = [];

    /// <summary>The character levels that each give one spell from any school.</summary>
    public List<int> AnySchoolSpellLevels { get; init; } = [];

    /// <summary>A draconic sorcerer's Draconic Resilience, a Dance bard's Unarmored Defense; null otherwise.</summary>
    public UnarmoredArmorClass? UnarmoredArmorClass { get; init; }
}

/// <summary>One entry of ClassFeatures.json or BackgroundFeatures.json.</summary>
public sealed record FeatureSeedRow
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public int Level { get; init; }
    public string? ClassId { get; init; }
    public string? SubclassId { get; init; }

    /// <summary>"class", "subclass" or "background".</summary>
    public required string Type { get; init; }
}
