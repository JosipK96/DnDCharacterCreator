namespace CharacterCreatorApp.Domain.DomainModels.Races;

/// <summary>
/// A 2014 subrace ("Hill Dwarf"), or one of a 2024 species' lineages or ancestries ("Drow
/// Lineage", a dragonborn's "Black").
/// </summary>
public class Subrace
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string RaceId { get; set; }

    /// <summary>Set only where the subrace changes its race's speed; see Race.SpeedWith.</summary>
    public int? Speed { get; set; }

    /// <summary>Languages picked on top of the race's: a high elf's extra language.</summary>
    public int LanguageChoiceCount { get; set; }

    /// <summary>
    /// Abilities the player raises by 1 in place of the race's choice: a variant human's two of
    /// all six. See Race.AbilityChoiceWith.
    /// </summary>
    public int AbilityChoiceCount { get; set; }

    public List<string> AbilityChoiceOptions { get; set; } = [];

    /// <summary>Skills picked on top of the race's: a variant human's one. Empty options mean any skill.</summary>
    public int SkillChoiceCount { get; set; }

    public List<string> SkillChoiceOptions { get; set; } = [];

    /// <summary>Feats picked on top of the race's: a variant human's one, of any category when FeatChoiceCategory is null.</summary>
    public int FeatChoiceCount { get; set; }

    public string? FeatChoiceCategory { get; set; }

    public Race Race { get; set; } = null!;
    public ICollection<SubraceAbilityBonus> AbilityBonuses { get; set; } = [];
    public ICollection<SubraceTrait> Traits { get; set; } = [];
    public ICollection<SubraceProficiency> SubraceProficiencies { get; set; } = [];
    public ICollection<SubraceSpell> Spells { get; set; } = [];
}
