using CharacterCreatorApp.Domain.DomainModels.Proficiencies;
using CharacterCreatorApp.Domain.DomainModels.Spells;

namespace CharacterCreatorApp.Infrastructure.Seed.Rows;

/// <summary>
/// One race of Races.json, written by the generator and read by RaceSeeder, with its subraces,
/// traits and everything it grants inside it. A 2024 species' lineages and ancestries ("Drow
/// Lineage", a dragonborn's "Black") are its subraces.
/// </summary>
public sealed record RaceSeedRow
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Edition { get; init; }

    /// <summary>The code of the book it comes from ("PHB", "XGE"), a row of Books.json.</summary>
    public required string Source { get; init; }
    public required string Size { get; init; }
    public int Speed { get; init; }

    /// <summary>Skills the player picks; no options means any skill.</summary>
    public int SkillChoiceCount { get; init; }

    public List<string> SkillChoiceOptions { get; init; } = [];
    public int LanguageChoiceCount { get; init; }

    /// <summary>Abilities the player raises by 1 besides the fixed bonuses: a half-elf's two.</summary>
    public int AbilityChoiceCount { get; init; }

    public List<string> AbilityChoiceOptions { get; init; } = [];

    /// <summary>Feats the race gives to pick; a category ("Origin") or null for any feat.</summary>
    public int FeatChoiceCount { get; init; }

    public string? FeatChoiceCategory { get; init; }

    /// <summary>Language names ("Common", "Dwarvish"), which LanguageResolver turns into ids.</summary>
    public List<string> Languages { get; init; } = [];

    public List<AbilityBonusSeedRow> AbilityBonuses { get; init; } = [];
    public List<string> ProficiencyIds { get; init; } = [];
    public List<TraitSeedRow> Traits { get; init; } = [];
    public List<GrantedSpellSeedRow> Spells { get; init; } = [];

    /// <summary>Spells the player picks from a class list; see Race.SpellChoiceWith.</summary>
    public BonusSpellChoice? SpellChoice { get; init; }

    public List<ToolChoice> ToolChoices { get; init; } = [];

    public List<SubraceSeedRow> Subraces { get; init; } = [];
}

public sealed record SubraceSeedRow
{
    public required string Id { get; init; }
    public required string Name { get; init; }

    /// <summary>The code of the book it comes from ("PHB", "XGE"), a row of Books.json.</summary>
    public required string Source { get; init; }

    /// <summary>Set only where the subrace changes its race's speed: a wood elf's 35 feet.</summary>
    public int? Speed { get; init; }

    /// <summary>Languages picked on top of the race's: a high elf's extra language.</summary>
    public int LanguageChoiceCount { get; init; }

    /// <summary>Abilities raised by 1 in place of the race's choice: a variant human's two of six.</summary>
    public int AbilityChoiceCount { get; init; }

    public List<string> AbilityChoiceOptions { get; init; } = [];

    /// <summary>Skills picked on top of the race's; no options means any skill.</summary>
    public int SkillChoiceCount { get; init; }

    public List<string> SkillChoiceOptions { get; init; } = [];

    /// <summary>Feats picked on top of the race's: a variant human's one, of any category.</summary>
    public int FeatChoiceCount { get; init; }

    public string? FeatChoiceCategory { get; init; }

    public List<AbilityBonusSeedRow> AbilityBonuses { get; init; } = [];
    public List<string> ProficiencyIds { get; init; } = [];
    public List<TraitSeedRow> Traits { get; init; } = [];
    public List<GrantedSpellSeedRow> Spells { get; init; } = [];

    /// <summary>Spells the player picks from a class list: a high elf's wizard cantrip.</summary>
    public BonusSpellChoice? SpellChoice { get; init; }
}

/// <summary>A fixed ability score increase, the ability as its canonical id ("CON").</summary>
public sealed record AbilityBonusSeedRow
{
    public required string AbilityScoreId { get; init; }
    public int Bonus { get; init; }
}

public sealed record TraitSeedRow
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
}

/// <summary>A spell given at a character level: a tiefling's Hellish Rebuke at 3.</summary>
public sealed record GrantedSpellSeedRow
{
    public required string SpellId { get; init; }
    public int Level { get; init; }
}
