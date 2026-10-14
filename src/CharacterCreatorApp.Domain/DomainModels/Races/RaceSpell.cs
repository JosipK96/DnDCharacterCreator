using CharacterCreatorApp.Domain.DomainModels.Spells;

namespace CharacterCreatorApp.Domain.DomainModels.Races;

/// <summary>A spell a race gives from a character level on: a tiefling's Hellish Rebuke at 3.</summary>
public class RaceSpell
{
    public required string RaceId { get; set; }
    public required string SpellId { get; set; }
    public int Level { get; set; }

    public Race Race { get; set; } = null!;
    public Spell Spell { get; set; } = null!;
}
