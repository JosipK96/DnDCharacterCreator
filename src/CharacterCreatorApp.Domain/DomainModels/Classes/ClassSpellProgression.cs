namespace CharacterCreatorApp.Domain.DomainModels.Classes;

public class ClassSpellProgression
{
    public required string ClassId { get; set; }
    public int CharacterLevel { get; set; }
    public int SpellsKnown { get; set; }     // 0 = prepared caster (no fixed limit)
    public int CantripsKnown { get; set; }
    public int MaxSpellLevel { get; set; }
    public Class Class { get; set; } = null!;
}