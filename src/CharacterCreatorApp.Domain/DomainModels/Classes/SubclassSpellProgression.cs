namespace CharacterCreatorApp.Domain.DomainModels.Classes;

public class SubclassSpellProgression
{
    public required string SubclassId { get; set; }
    public int CharacterLevel { get; set; }
    public int SpellsKnown { get; set; }
    public int CantripsKnown { get; set; }
    public int MaxSpellLevel { get; set; }
    public required string SpellcastingAbility { get; set; }
    public Subclass Subclass { get; set; } = null!;
}