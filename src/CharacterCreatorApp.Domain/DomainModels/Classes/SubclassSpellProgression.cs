namespace CharacterCreatorApp.Domain.DomainModels.Classes;

public class SubclassSpellProgression
{
    public required string SubclassId { get; set; }
    public int CharacterLevel { get; set; }
    public int SpellsKnown { get; set; }
    public int CantripsKnown { get; set; }
    public int MaxSpellLevel { get; set; }

    /// <summary>A third caster's spell slots per spell level, index 0 for 1st level.</summary>
    public List<int> SpellSlots { get; set; } = [];

    /// <summary>Pact Magic slots, all of MaxSpellLevel; 0 for every subclass the data has.</summary>
    public int PactMagicSlots { get; set; }

    public required string SpellcastingAbility { get; set; }
    public Subclass Subclass { get; set; } = null!;
}
