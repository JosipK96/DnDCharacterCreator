namespace CharacterCreatorApp.Domain.DomainModels.Classes;

public class ClassSpellProgression
{
    public required string ClassId { get; set; }
    public int CharacterLevel { get; set; }
    public int SpellsKnown { get; set; }     // 0 = prepared caster (no fixed limit)
    public int CantripsKnown { get; set; }
    public int MaxSpellLevel { get; set; }

    /// <summary>The spell slots per spell level, index 0 for 1st level; empty for a warlock.</summary>
    public List<int> SpellSlots { get; set; } = [];

    /// <summary>A warlock's Pact Magic slots, all of MaxSpellLevel; 0 for every other class.</summary>
    public int PactMagicSlots { get; set; }

    public Class Class { get; set; } = null!;
}
