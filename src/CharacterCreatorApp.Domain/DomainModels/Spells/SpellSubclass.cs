using CharacterCreatorApp.Domain.DomainModels.Classes;

namespace CharacterCreatorApp.Domain.DomainModels.Spells;

/// <summary>A spell a subclass gives: always prepared, or only added to what its class may pick.</summary>
public class SpellSubclass
{
    public int Id { get; set; }
    public required string SpellId { get; set; }
    public required string SubclassId { get; set; }

    /// <summary>The character level from which the subclass gives the spell.</summary>
    public int Level { get; set; }

    /// <summary>
    /// True for a spell the character has without picking it, such as a cleric's domain spells;
    /// false for one the subclass adds to the list the character picks from, such as a 2014
    /// warlock patron's expanded spells.
    /// </summary>
    public bool IsAlwaysPrepared { get; set; }

    public Spell Spell { get; set; } = null!;
    public Subclass Subclass { get; set; } = null!;
}
