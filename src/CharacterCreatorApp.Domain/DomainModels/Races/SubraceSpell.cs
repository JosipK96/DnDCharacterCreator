using CharacterCreatorApp.Domain.DomainModels.Spells;

namespace CharacterCreatorApp.Domain.DomainModels.Races;

/// <summary>A spell a subrace or lineage gives from a character level on: a drow's Faerie Fire at 3.</summary>
public class SubraceSpell
{
    public required string SubraceId { get; set; }
    public required string SpellId { get; set; }
    public int Level { get; set; }

    public Subrace Subrace { get; set; } = null!;
    public Spell Spell { get; set; } = null!;
}
