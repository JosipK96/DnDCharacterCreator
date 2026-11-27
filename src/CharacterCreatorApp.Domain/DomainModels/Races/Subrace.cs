using CharacterCreatorApp.Domain.DomainModels.Spells;

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

    /// <summary>The code of the book it comes from ("PHB", "XGE"); see Books.json in the seed data.</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>Set only where the subrace changes its race's speed; see Race.SpeedWith.</summary>
    public int? Speed { get; set; }

    /// <summary>Languages picked on top of the race's: a high elf's extra language.</summary>
    public int LanguageChoiceCount { get; set; }

    /// <summary>
    /// The points the player spreads in place of the race's choice: a variant human's two +1s of
    /// all six, a SCAG tiefling variant's +2 to Dexterity or Charisma. See Race.AbilityChoiceWith.
    /// </summary>
    public int AbilityChoiceCount { get; set; }

    public List<string> AbilityChoiceOptions { get; set; } = [];

    /// <summary>The most of those points one ability takes; see Race.AbilityChoiceMaxPerAbility.</summary>
    public int AbilityChoiceMaxPerAbility { get; set; } = 1;

    /// <summary>The fewest points an ability it raises takes; see Race.AbilityChoiceMinPerAbility.</summary>
    public int AbilityChoiceMinPerAbility { get; set; } = 1;

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

    /// <summary>Spells the player picks from a class list: a high elf's wizard cantrip.</summary>
    public BonusSpellChoice? SpellChoice { get; set; }

    /// <summary>The ability its own spells are cast with, by canonical id: a drow's "CHA"; see Race.SpellcastingAbility.</summary>
    public string? SpellcastingAbility { get; set; }
}
